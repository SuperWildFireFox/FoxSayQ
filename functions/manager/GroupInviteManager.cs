using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirai.Net.Data.Events.Concretes.Request;
using Mirai.Net.Data.Shared;
using Mirai.Net.Sessions.Http.Managers;

namespace FoxSayQ.functions.manager
{
  //单例模式
  //群邀请管理器
  public sealed class GroupInviteManager
  {
    private static GroupInviteManager? instance = null;
    private static readonly object padlock = new object();
    private GroupInviteManager()
    {

    }
    public static GroupInviteManager Instance
    {
      get
      {
        lock (padlock)
        {
          if (instance == null)
          {
            instance = new GroupInviteManager();
          }
          return instance;
        }
      }
    }
    //超级管理员账户
    private String manager_qq_num = Program.main_config.robotConfig.manager_qq_num;

    //超时取消时间 mins
    public readonly int INVITE_TIME_OUT = 60;

    //TODO:这里不严谨，但是对纯单一功能来说很合适
    //自增的id
    private int _unique_request_id = 0;
    private int unique_request_id
    {
      get
      {
        _unique_request_id += 1;
        return _unique_request_id;
      }
    }


    private Dictionary<int, Tuple<NewInvitationRequestedEvent, TaskCompletionSource>> WaitingRequestDict = new Dictionary<int, Tuple<NewInvitationRequestedEvent, TaskCompletionSource>>();

    //检查输入是否由该插件处理
    public async Task<bool> CheckIfHandleAsync(String FromId, String msg)
    {
      bool check_result = false;
      if (FromId == manager_qq_num)
      {
        string[] result = msg.Trim().Split(" ");
        int request_id;
        if (int.TryParse(result[0], out request_id) && WaitingRequestDict.ContainsKey(request_id))
        {
          check_result = true;
        }
      }
      if (check_result)
      {
        await HandleResultAsync(msg);
      }
      return check_result;
    }


    //同意或拒接加群申请
    private async Task HandleResultAsync(String msg)
    {
      string[] result = msg.Trim().Split(" ");
      int request_id = int.Parse(result[0]);
      String command = result[1];
      NewInvitationRequestedEvent e = WaitingRequestDict[request_id].Item1;
      if (command == "lgtm")
      {
        await RequestManager.HandleNewInvitationRequestedAsync(e, NewInvitationRequestHandlers.Approve, "");
        await MessageManager.SendFriendMessageAsync(manager_qq_num, $"{request_id} 申请已批准");
      }
      else
      {
        await RequestManager.HandleNewInvitationRequestedAsync(e, NewInvitationRequestHandlers.Reject, command);
        await MessageManager.SendFriendMessageAsync(manager_qq_num, $"{request_id} 已拒绝该申请");
      }
      //终止等待
      var tcs = WaitingRequestDict[request_id].Item2;
      tcs.SetResult();
    }

    private async Task SendRequestForManager(NewInvitationRequestedEvent e,int request_id)
    {
      // CancellationToken cancellationToken,
      String invite_person = e.FromId;
      String invite_person_name = e.Nick;
      String invited_group = e.GroupId;
      String other_messgae = e.Message;
      String message = $"{request_id} 用户 {invite_person_name}({e.FromId})邀请狐语加入群{e.GroupId},是否同意？";
      //将该请求加入到字典中
      var tcs = new TaskCompletionSource();
      WaitingRequestDict.Add(request_id, new Tuple<NewInvitationRequestedEvent, TaskCompletionSource>(e, tcs));
      await MessageManager.SendFriendMessageAsync(manager_qq_num, message);

      //开始等待任务完成
      await tcs.Task;
    }

    public async Task SendRequestForManagerWithTimeout(NewInvitationRequestedEvent e){
      using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(INVITE_TIME_OUT));
      int now_request_id = unique_request_id;
      try
      {
          // 向管理员询问是否同意
          await SendRequestForManager(e,now_request_id);
      }
      catch (OperationCanceledException)
      {
          await MessageManager.SendFriendMessageAsync(manager_qq_num, $"{now_request_id} 该申请已过期");
      }
      finally{
        //释放字典
        WaitingRequestDict.Remove(now_request_id);
      }
    }
  }
}