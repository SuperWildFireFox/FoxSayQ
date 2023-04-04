using System.Reactive.Linq;
using Manganese.Text;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;

namespace FoxSayQ
{
  public class Program
  {
    public static async Task Main()
    {
      var bot = new MiraiBot
      {
        Address = "localhost:8080",
        QQ = "3221734968",
        VerifyKey = "9he&jdsofguw34"
      };

      // 注意: `LaunchAsync`是一个异步方法，请确保`Main`方法的返回值为`Task`
      await bot.LaunchAsync();

      bot.MessageReceived
          .OfType<GroupMessageReceiver>()
          .Subscribe(x =>
          {
            new HandleMessage().HandleGroupMessage(x);
          });

      bot.MessageReceived
          .OfType<FriendMessageReceiver>()
          .Subscribe(x =>
          {
            Console.WriteLine($"收到了来自好友{x.FriendId}发送的消息：{x.MessageChain.GetPlainMessage()}");
          });

      bot.MessageReceived
          .OfType<StrangerMessageReceiver>()
          .Subscribe(x =>
          {
            Console.WriteLine($"收到了来自陌生人{x.StrangerId}发送的消息：{x.MessageChain.GetPlainMessage()}");
          });
      Console.WriteLine("Robot ready");
      while (true)
      {
        string? command = Console.ReadLine();
        if (command == null)
        {
          continue;
        }
        else if (command == "exit" || command == "e")
        {
          Console.WriteLine("Bye");
          break;
        }
        else if (command == "add")
        {
          string? group = Console.ReadLine();
        }
      }
    }
  }
}