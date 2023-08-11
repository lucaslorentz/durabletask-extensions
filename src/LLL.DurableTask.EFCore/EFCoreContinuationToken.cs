using System;
using System.Text;
using Newtonsoft.Json;

namespace LLL.DurableTask.EFCore;

public class EFCoreContinuationToken
{
    public DateTime CreatedTime { get; set; }
    public string InstanceId { get; set; }

    public static EFCoreContinuationToken Parse(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return JsonConvert.DeserializeObject<EFCoreContinuationToken>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
    }

    public string Serialize()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
    }
}
