using System.Reactive.Linq;
using FoxSayQ.functions.manager;
using FoxSayQ.handlers;
using FoxSayQ.utils;
using Manganese.Text;
using Mirai.Net.Data.Events.Concretes.Request;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Data.Shared;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Newtonsoft.Json;

namespace FoxSayQ
{
  public class Program
  {
    public static MainConfig main_config = Config.ReadConfig<MainConfig>("configs/main_config.json");

    public static void Main_()
    {
      Console.WriteLine(main_config.robotConfig.mirai_address);
    }
    public static async Task Main()
    {
      var bot = new MiraiBot
      {
        Address = main_config.robotConfig.mirai_address,
        QQ = main_config.robotConfig.robot_qq_num,
        VerifyKey = main_config.robotConfig.mirai_vertify
      };

      // 注意: `LaunchAsync`是一个异步方法，请确保`Main`方法的返回值为`Task`
      await bot.LaunchAsync();

      GroupMessageHandler groupMessageHandler = new GroupMessageHandler(main_config.robotConfig.robot_qq_num);
      FriendMessageHandler friendMessageHandler = new FriendMessageHandler(main_config.robotConfig.robot_qq_num);

      //群组事件监听
      bot.MessageReceived
          .OfType<GroupMessageReceiver>()
          .Subscribe(x =>
          {
            groupMessageHandler.Handle(x);
          });

      //好友事件监听
      bot.MessageReceived
          .OfType<FriendMessageReceiver>()
          .Subscribe(x =>
          {
            friendMessageHandler.Handle(x);
          });

      //陌生人事件监听
      bot.MessageReceived
          .OfType<StrangerMessageReceiver>()
          .Subscribe(x =>
          {
            Console.WriteLine($"收到了来自陌生人{x.StrangerId}发送的消息：{x.MessageChain.GetPlainMessage()}");
          });

      GroupInviteManager groupInviteManager = GroupInviteManager.Instance;
      //邀请入群监听
      bot.EventReceived
        .OfType<NewInvitationRequestedEvent>()
        .Subscribe(async e =>
        {
          await groupInviteManager.SendRequestForManagerWithTimeout(e);
        });

      bot.EventReceived
      .OfType<NewFriendRequestedEvent>()
      .Subscribe(async e =>
      {
        string message = $"{e.Nick}({e.FromId})邀请小狐狸作为他的朋友\n{e.Message}";
        await MessageManager.SendFriendMessageAsync(main_config.robotConfig.manager_qq_num, message);
        //传统的方式
        await RequestManager.HandleNewFriendRequestedAsync(e, NewFriendRequestHandlers.Approve);
     });


      Console.WriteLine("机器人已就绪");

      //卡住主线程，让子线程循环
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