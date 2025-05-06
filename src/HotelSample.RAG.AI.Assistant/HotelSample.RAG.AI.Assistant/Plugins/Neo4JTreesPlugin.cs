using HotelSample.RAG.AI.Assistant.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
 
        /*
        //Version Case Sensitive
        string _programasRelacionadosQuery = """
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
        */

        //Version Case insensitive
        string _programasRelacionadosQuery = """
            MATCH path = (a)-[r:INVOCA_A*]->(b)
            WHERE toLower(b.name) = toLower('%PROGRAMA%')
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

        [KernelFunction, Description("Analiza que programas utilizan directa o indirectamente el programa solicitado. " +
            "Se ejecuta cuando el usuario pregunta por los programas que utilizan o invocan un determinado programa " +
            ", o bien cuando pide un 'Analisis de Impacto' de un determinado programa.\n" +
            "No generes una respuesta para el usuario. Esta función es silenciosa y solo ejecuta una acción interna.")]
        public async Task<object> BuscarDependencias(
            [Description("El nombre del programa al cual buscar programas dependientes de el")] string nombrePrograma)
        {
            Console.WriteLine("...consultando con sistema A.B.E.L. ...\n");
            
            string cypherQuery = _programasRelacionadosQuery.Replace("%PROGRAMA%", nombrePrograma);
            Neo4jRequest neo4JRequest = new Neo4jRequest(cypherQuery);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string jsonContent = JsonSerializer.Serialize(neo4JRequest, options);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(NEO4J_URL, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(responseBody);

            List<NodoNeo4J> arbol = ParseTreeStructure(data);

            if (arbol.Count == 0)
            {
                return new
                {
                    Success = false,
                    result = $"No se detectaron programas que dependan del programa {nombrePrograma}"
                };
            }

            Tree arbolAscii = ConstruirArbolAscii(arbol);
            ConsoleColor color = Console.ForegroundColor;
            AnsiConsole.Write(arbolAscii);
            Console.WriteLine();
            Console.ForegroundColor = color;

            var sb = new StringBuilder();
            var pathActual = new List<string>();
            int contador = 0;
//            string explicacion = ConvertirArbolATextoMarkdownMasExplicado(arbol);
            GenerarPathsHastaHojasInvertido(arbol[0], pathActual, sb, ref contador);
            //string explicacionEnMarkDown = $"Los caminos de invocacion son ('->' se interpreta como llama, usa o invoca a):" + sb.ToString(); //ConvertirArbolATextoMarkdownMasExplicado(arbol);

            string explicacionEnMarkDown = $"Los caminos de invocacion son ('->' se interpreta como llama, usa, invoca a, afecta a):" + ConvertirArbolATextoMarkdownMasExplicado(arbol)
                + "\n Tener en cuenta que si un programa 'A' afecta a otro 'B', la inversa no es valida";

            string respuesta = $"Programas que dependen de {nombrePrograma}:\n {explicacionEnMarkDown}.";
            //            respuesta += "\nEsta respuesta NO debe ser mostrada al usuario. Este plugin completa por pantalla la informacion necesaria.";

            //            string respuesta = $"La representacion grafica para mostrar es (no trates de interpretarlo" +
            //                $", es solo un esquma grafico): {CapturarTextoTree(arbolAscii)}\n Las relaciones se interpretan asi: {explicacionEnMarkDown}." +
            //                $"\n *Recuerda, si dos programas no comparten un camino en comun, no hay forma que tengan relacion de dependencia entre ellos*";

            //string respuesta = $"Los caminos de invocacion son: {explicacionEnMarkDown}.";

            //            Console.WriteLine(explicacionEnMarkDown);

            //Console.WriteLine(sb.ToString());

            return new
            {
                Success = true,
                result = respuesta
            };
        }


        private static List<NodoNeo4J> ParseTreeStructure(JObject data)
        {
            if (data["results"] == null || data["results"].Count() == 0)
                return new List<NodoNeo4J>();

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
                        , nodePadre?["objetivo"]?.ToString()
                        , labelsPadre
                        );

                    NodoNeo4J nodoNeo4JHijo = new NodoNeo4J(
                        idHijo?.ToString()
                        , nodoNeo4JPadre.Id
                        , nodeHijo?["name"]?.ToString()
                        , nodeHijo?["environment"]?.ToString()
                        , nodeHijo?["proceso"]?.ToString()
                        , nodeHijo?["objetivo"]?.ToString()
                        , labelsHijo
                        );

                    nodosPadres.Add(nodoNeo4JPadre);
                    nodosHijos.Add(nodoNeo4JHijo);
                }
            }

            //De entre todos los nodos padres, me quedo con el nodo Raiz
            //(el unico padre que no tiene padre)
            NodoNeo4J? nodoRaiz = nodosPadres.FirstOrDefault(n => n.IdPadre == null);
            if (nodoRaiz == null) return new List<NodoNeo4J>();

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


        static string ConvertirArbolATextoMarkdownMasExplicado(List<NodoNeo4J> arbol)
        {
            NodoNeo4J raiz = arbol.First();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"## Explicacion de las invocaciones del programa '{raiz.Name}'");
            bool hayNivelesMayoresA2 = false;

            if (raiz.Hijos.Count == 0)
                return "El programa no es invocado por nadie";


            //sb.AppendLine($"### Nivel 1: Invocaciones directas a '{raiz.Name}'");
            sb.AppendLine($"### Nivel 1: Afectado directo por '{raiz.Name}'");

            foreach (NodoNeo4J hijoDirecto in raiz.Hijos)
            {
                sb.AppendLine($"- {hijoDirecto.Name}");

                if (hijoDirecto.Hijos.Count > 0)
                { 
                    hayNivelesMayoresA2 = true;
                }
            }

            if (hayNivelesMayoresA2)
            {
                sb.AppendLine($"### Nivel 2 y mayores:");

                foreach (NodoNeo4J hijoDirecto in raiz.Hijos)
                {
                    CompletarExplicacionNivelesBajos(hijoDirecto, ref sb);
                }
            }

            return sb.ToString();
        }


        void GenerarPathsHastaHojas(NodoNeo4J nodo, List<string> pathActual, StringBuilder sb, ref int contador)
        {
            pathActual.Add(nodo.Name!); 

            if (nodo.Hijos == null || nodo.Hijos.Count == 0)
            {
                contador++;
                sb.AppendLine($"Camino Nro {contador}: " + string.Join(" es invocado por ", pathActual));
            }
            else
            {
                foreach (var hijo in nodo.Hijos)
                {
                    GenerarPathsHastaHojas(hijo, pathActual, sb, ref contador);
                }
            }

            pathActual.RemoveAt(pathActual.Count - 1); // Retroceder en el path
        }

        void GenerarPathsHastaHojasInvertido(NodoNeo4J nodo, List<string> pathActual, StringBuilder sb, ref int contador)
        {
            pathActual.Add(nodo.Name!);

            if (nodo.Hijos == null || nodo.Hijos.Count == 0)
            {
                contador++;
                //sb.AppendLine($"Camino Nro {contador}: " + string.Join(" invoca/usa/llama a ", pathActual.AsEnumerable().Reverse()));
                sb.AppendLine($"Camino Nro {contador}: " + string.Join(" -> ", pathActual.AsEnumerable().Reverse()));
            }
            else
            {
                foreach (var hijo in nodo.Hijos)
                {
                    GenerarPathsHastaHojasInvertido(hijo, pathActual, sb, ref contador);
                }
            }

            pathActual.RemoveAt(pathActual.Count - 1); // Retroceder en el path
        }


        static string ConvertirArbolAMarkDownConPaths(List<NodoNeo4J> arbol)
        {
            NodoNeo4J raiz = arbol.First();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"## Explicacion de las invocaciones del programa '{raiz.Name}'");
            bool hayNivelesMayoresA2 = false;

            if (raiz.Hijos.Count == 0)
                return "El programa no es invocado por nadie";


            sb.AppendLine($"### Nivel 1: Invocaciones directas a '{raiz.Name}'");

            foreach (NodoNeo4J hijoDirecto in raiz.Hijos)
            {
                sb.AppendLine($"- {hijoDirecto.Name}");

                if (hijoDirecto.Hijos.Count > 0)
                {
                    hayNivelesMayoresA2 = true;
                }
            }

            if (hayNivelesMayoresA2)
            {
                sb.AppendLine($"### Nivel 2 y mayores:");

                foreach (NodoNeo4J hijoDirecto in raiz.Hijos)
                {
                    CompletarExplicacionNivelesBajos(hijoDirecto, ref sb);
                }
            }

            return sb.ToString();
        }

        static void CompletarExplicacionNivelesBajos(NodoNeo4J nodo, ref StringBuilder sb)
        {
            foreach (NodoNeo4J nodoHijo in nodo.Hijos)
            {
                //sb.AppendLine($"- {nodoHijo.Name} utiliza/invoca a {nodo.Name}");
                sb.AppendLine($"- {nodo.Name} afecta a {nodoHijo.Name}");
                CompletarExplicacionNivelesBajos(nodoHijo, ref sb);
            }
        }


        //No hizo falta terminarla
        static string RecorrerPorNiveles(NodoNeo4J raiz)
        {
            StringBuilder sb = new StringBuilder();

            var cola = new Queue<(NodoNeo4J nodo, int nivel)>();
            cola.Enqueue((raiz, 0));
            NodoNeo4J nodoPadre;

            while (cola.Count > 0)
            {
                var (nodo, nivel) = cola.Dequeue();

                sb.AppendLine($"### Nivel {nivel}:");

                Console.WriteLine($"- {nodo.Name}");
                nodoPadre = nodo;
                foreach (var hijo in nodo.Hijos)
                {
                    cola.Enqueue((hijo, nivel + 1));
                }
            }

            return sb.ToString();
        }

        ///Consulta de relaciones entre 2 programas X e Y. Existe algun camino posible entre ellos? Si lo hay,
        ///quien depende de quien?
        
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
