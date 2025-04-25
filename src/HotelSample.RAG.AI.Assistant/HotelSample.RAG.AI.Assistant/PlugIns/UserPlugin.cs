using HotelSample.RAG.AI.Assistant.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class UserPlugin
    {
        [KernelFunction("user_info")]
        [Description("When the user introduces himself, use this function to obtain the information")]

        public UserInfo GetUserInfo(
            [Description("the firstname of the user. Convert the firstname to uppercase")] string firstName,
            [Description("the lastname of the user")] string lastName,
            [Description("the email of the user")] string email,
            [Description("indicates the user's birthday. Convert what the user enters to the dd/MM/yyyy format.")] string birthday)
        {
            UserInfo userInfo = new UserInfo()
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Birthday = DateTime.ParseExact(birthday, "dd/MM/yyyy", null)
            };

            return userInfo;
        }
    }

}