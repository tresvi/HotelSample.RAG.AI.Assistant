using HotelSample.RAG.AI.Assistant.Models;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class Neo4JTreesPlugin
    {
        private const string NEO4J_URL = "http://localhost:7474/db/neo4j/tx/commit";

        private static readonly HttpClient _httpClient = new HttpClient();

        string _programasRelacionosQuery = """
            MATCH path = (a)-[r:INVOCA_A*]->(b {name: '%PROGRAMA%'})
            UNWIND relationships(path) AS rel
            WITH rel, startNode(rel) AS start, endNode(rel) AS end
            RETURN DISTINCT
                rel.name AS Relacion,
                elementId(start) AS IdStart,
                labels(start) AS LabelsStart,
                start AS NodoStart,
                elementId(end) AS IdEnd,
                labels(end) AS LabelsEnd,
                end AS NodoEnd
            """;

        [KernelFunction, Description("Busca en la base Neo4J relaciones de dependencias de un programa. Se ejecuta cuando el usuario pregunta por los programas que dependen de un determinado programa")]
        public async Task<string> BuscarDependencias(
            [Description("El nombre del programa a buscar las dependencias")] string nombrePrograma)
        {
            Console.WriteLine("...consutando con sistema A.B.E.L. ...\n");

            string cypherQuery = _programasRelacionosQuery.Replace("%PROGRAMA%", nombrePrograma);
            Neo4jRequest neo4JRequest = new Neo4jRequest(cypherQuery);

            //var content = new StringContent(JsonSerializer.Serialize(_programasRelacionosQuery), Encoding.UTF8, "application/json");
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string jsonContent = JsonSerializer.Serialize(neo4JRequest, options);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(NEO4J_URL, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(responseBody);

            List<NodoNeo4J> arbol = ParseTreeStructure(data);
            Tree arbolAscii = ConstruirArbolAscii(arbol);
            AnsiConsole.Write(arbolAscii);
            Console.WriteLine();
            return ConvertirArbolATextoMarkdown(CapturarTextoTree(arbolAscii));
        }


        private static List<NodoNeo4J> ParseTreeStructure(JObject data)
        {
            List<NodoNeo4J> nodosPadres = new List<NodoNeo4J>();
            List<NodoNeo4J> nodosHijos = new List<NodoNeo4J>();

            foreach (var result in data["results"])
            {
                foreach (var row in result["data"])
                {
                    var idHijo = row["row"]?[1];
                    var labelsHijo = row["row"]?[2]?.ToObject<List<string>>();
                    var nodeHijo = row["row"]?[3];

                    var idPadre = row["row"]?[4];
                    var labelsPadre = row["row"]?[5]?.ToObject<List<string>>();
                    var nodePadre = row["row"]?[6];

                    NodoNeo4J nodoNeo4JPadre = new NodoNeo4J(
                        idPadre?.ToString()
                        , null
                        , nodePadre?["name"]?.ToString()
                        , nodePadre?["environment"]?.ToString()
                        , nodePadre?["proceso"]?.ToString()
                        , labelsPadre
                        );

                    NodoNeo4J nodoNeo4JHijo = new NodoNeo4J(
                        idHijo?.ToString()
                        , nodoNeo4JPadre.Id
                        , nodeHijo?["name"]?.ToString()
                        , nodeHijo?["environment"]?.ToString()
                        , nodeHijo?["proceso"]?.ToString()
                        , labelsHijo
                        );

                    nodosPadres.Add(nodoNeo4JPadre);
                    nodosHijos.Add(nodoNeo4JHijo);
                }
            }

            //De entre todos los nodos padres, me quedo con el nodo Raiz
            //(el unico padre que no tiene padre)
            NodoNeo4J nodoRaiz = nodosPadres.First(n => n.IdPadre == null);

            //Elimino los ciclos cerrados de la Raiz (si los tiene)
            int nroCiclosEnRaiz = nodosHijos.RemoveAll(x => x.Id == nodoRaiz.Id);

            (List<NodoNeo4J> arbol, _ ) = ConvertirAArbol(nodoRaiz, nodosHijos);
            return arbol;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodoRaiz"></param>
        /// <param name="nodosHijos"></param>
        /// <param name="repetirNodosConMultiplesPadres">En False, recorta relaciones de nodos con multiples padres. Estos producen repeticiones de ramas</param>
        /// <returns></returns>
        private static (List<NodoNeo4J> arbol, bool fueSimplficada) ConvertirAArbol(NodoNeo4J nodoRaiz, List<NodoNeo4J> nodosHijos, bool repetirNodosConMultiplesPadres = false)
        {
            List<NodoNeo4J> arbol = [nodoRaiz, .. nodosHijos];

            bool arbolFueSimplificado = false;

            if (!repetirNodosConMultiplesPadres)
            {
                //Elimino (si los hay) nodos con multiples padres para evitar ramificaciones con nodos
                //duplicados. No esta necesariamente mal, pero esta situacion duplica ramas.
                //Si hubo nodos filtrados, significa que se simplificó la estructura
                int nroNodosAntesDeFiltrar = arbol.Count;
                arbol = arbol.DistinctBy(x => x.Id).ToList();
                arbolFueSimplificado = nroNodosAntesDeFiltrar > arbol.Count;
            }

            //Recorro todos los nodos, relacionandolos con sus hijos (si los tiene)
            //para armar la estructura de arbol
            foreach (NodoNeo4J nodo in arbol)
            {
                nodo.Hijos = arbol.Where(x => x.IdPadre == nodo.Id && x.Id != nodoRaiz.Id).ToList();
            }
            return (arbol, arbolFueSimplificado);
        }


        static Tree ConstruirArbolAscii(List<NodoNeo4J> arbol)
        {
            NodoNeo4J raiz = arbol.First();
            var tree = new Tree($"[White]{raiz.Name!}[/]");
            AgregarHijos(tree, raiz);
            return tree;
        }


        static void AgregarHijos(IHasTreeNodes parentNode, NodoNeo4J nodo)
        {
            foreach (var hijo in nodo.Hijos)
            {
                TreeNode nodoHijo = parentNode.AddNode(hijo.ToStringConsole(true));
                AgregarHijos(nodoHijo, hijo);
            }
        }


        static string CapturarTextoTree(Tree tree)
        {
            var sw = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,  // Sin colores
                Out = new AnsiConsoleOutput(sw)
            });

            console.Write(tree);

            string rawText = sw.ToString();
            return rawText;
        }


        static string ConvertirArbolATextoMarkdown(string arbolTexto)
        {
            var lineas = arbolTexto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var resultado = new List<string>();

            foreach (var linea in lineas)
            {
                // Detectar la parte de indentación
                int i = 0;
                while (i < linea.Length && (linea[i] == ' ' || linea[i] == '│' || linea[i] == '├' || linea[i] == '└' || linea[i] == '─'))
                    i++;

                int nivelIndentado = i / 4;

                // Extraer y limpiar el texto del nodo
                var textoNodo = linea.Trim().TrimStart('├', '─', '└', '│', ' ');

                // Aplicar indentación correcta (sin el +1 de antes)
                resultado.Add($"{new string(' ', nivelIndentado * 2)}* {textoNodo}");
            }

            return string.Join("\n", resultado);
        }


        /*
         A.B.E.L. — Análisis Basado en Estructuras Lógicas
            Alternativas con enfoque técnico:
            A.B.E.L. — Análisis de Bases y Estructuras Lógicas
            A.B.E.L. — Arquitectura de Búsqueda y Evaluación Lógica
            A.B.E.L. — Análisis de Bloques y Entidades Lógicas
            A.B.E.L. — Administrador de Bases y Entrelazado Lógico
            A.B.E.L. — Análisis Basado en Esquemas de Lógica

            Con un toque más creativo:
            A.B.E.L. — Algoritmo de Búsqueda y Evaluación de Lazos (por las dependencias circulares)
            A.B.E.L. — Atlas de Bloques y Entidades Ligadas
            A.B.E.L. — Análisis de Bifurcaciones y Enlaces Lógicos
         */
    }
}
