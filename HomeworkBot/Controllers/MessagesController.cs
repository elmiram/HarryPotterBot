using Microsoft.ProjectOxford.Linguistics;
using Microsoft.ProjectOxford.Linguistics.Contract;
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeworkBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var help = "/help";

                if (activity.Text == help)
                {
                    Activity reply3 = activity.CreateReply("Please, read about this bot at http://harrypotterresponsebot.azurewebsites.net/ \n\n\n\nSend your message to the Harry Potter Bot! It will respond with a morphologically similar phrase. \n\n\n\nThe words for phases are taken from Harry Potter series. \n\n\n\nThis bot understands emojis! Try sending :blush: or :heart: =)");
                    await connector.Conversations.ReplyToActivityAsync(reply3);
                }

                else
                {
                    var Client = new LinguisticsClient("ec544bdca14645cf8d6454e8dc180836");
                    var Analyzers = await Client.ListAnalyzersAsync();

                    var Req = new AnalyzeTextRequest();
                    Req.Language = "en";
                    Req.Text = activity.Text;
                    Req.AnalyzerIds = new Guid[] { Analyzers[0].Id, Analyzers[2].Id };

                    var Res = await Client.AnalyzeTextAsync(Req);
                    var analyzerResponse = Res[0].Result.ToString();

                    // сначала отправляю юзеру разбор по частям речи,а он не отправляется
                    //Activity reply = activity.CreateReply($"{analyzerResponse}");
                    //await connector.Conversations.ReplyToActivityAsync(reply);

                    Regex ItemRegex = new Regex("\"(.*?)\"", RegexOptions.Compiled);
                    Regex RawTokenRegex = new Regex("\"RawToken\": \"(.*?)\"", RegexOptions.Compiled);
                    var posWords = new POSDict();

                    string resp = "";
                    MatchCollection messagePOS = ItemRegex.Matches(analyzerResponse);
                    MatchCollection messageRaw = RawTokenRegex.Matches(Res[1].Result.ToString());

                    for (var i = 0; i < messagePOS.Count; i++)
                    {
                        bool RawTokenCapitalized = char.IsUpper(messageRaw[i].Groups[1].ToString()[0]);
                        resp += posWords.getWord(messagePOS[i].Groups[1].ToString(), RawTokenCapitalized) + ' ';
                    }

                    // todo  
                    Activity reply2 = activity.CreateReply($"{resp}");
                    await connector.Conversations.ReplyToActivityAsync(reply2);
                }

                
                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }

    public class POSDict
    {

        public Dictionary<string, string[]> dict = CreateDictionary();
        

        static Dictionary<string, string[]> CreateDictionary()
        {
            Dictionary<string, string[]> wordDictionary = new Dictionary<string, string[]>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HPwords.txt");
            var f = File.OpenText(path);

            while (!f.EndOfStream)
            {
                String line = f.ReadLine();

                if (line.Length > 0)
                {
                    string[] words = line.Split(' ').ToArray();
                    wordDictionary.Add(words[0], words.Skip(1).ToArray());
                }
            }
            return wordDictionary;
        }

        public string getWord(string pos, bool IsCap)
        {
            if (dict.ContainsKey(pos))
            {
                var word = dict[pos].PickRandom();
                if (IsCap == true)
                {
                    word = FirstCharToUpper(word);
                }
                return word;
            }
            
            return string.Empty;
            
        }

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }
    }

    public static class EnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }
    }
}