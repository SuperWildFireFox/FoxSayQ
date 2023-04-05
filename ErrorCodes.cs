using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxSayQ
{
  public static class ErrorCodes
  {
    public static class ChatGPTErrorCode
    {
      public static readonly string ERROR_EMPTY_RESPOND_OBJECT =
        "Error: response 解析为空";
      public static readonly string ERROR_API_REQUEST_FAILED =
      "Error: API请求失败，返回信息：";
    }
  }
}