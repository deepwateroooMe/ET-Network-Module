using System;
using UnityEngine;
using static ET.TimerManager;
namespace ET {
    public class Ping { // 这是，心跳包吗？心跳包单独独立，为了消息模块的解偶。这个单独会有依赖的模块，怎么依赖的（消息内容？），把这个模块看懂【之前还没看过】
        public C2G_Ping C2G_Ping = new C2G_Ping();

        public long Id { get; set; }
        public long InstanceId { get; private set; }
        public static TimeInfo TimeInfo => TimeInfo.Instance;
        public Action<long> OnPingRecalculated;
        public bool IsDisposed => InstanceId == 0;
        public long delay; // 延迟值
        Session session;

        public Ping(Session session) {
            InstanceId = IdGenerater.Instance.GenerateInstanceId();
            this.session = session;
            _ = PingAsync();
        }
        public void Dispose() {
            if (this.IsDisposed) {
                return;
            }
            InstanceId = 0;
        }
        private async ETTask PingAsync() {
            long instanceId = InstanceId;
            while (true) {
                if (InstanceId != instanceId) {
                    return;
                }
                long time1 = TimeHelper.ClientNow();
                try {
                    G2C_Ping response = await session.Call(C2G_Ping) as G2C_Ping;
                    if (InstanceId != instanceId) {
                        return;
                    }
                    long time2 = TimeHelper.ClientNow();
                    delay = time2 - time1;
                    OnPingRecalculated?.Invoke(delay);
                    TimeInfo.ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2;
                    // Debug.Log($"{nameof(Ping)}:  ping = {delay} - {response.Time} - {response.Message} - {TimeInfo.ServerFrameTime()}");
                    await WaitAsync(2000); // 这就算是心跳包了：每隔2 秒发一次，关切消息 !!!
                }
                catch (RpcException e) {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Debug.Log($"ping error: {Id} {e.Error}");
                    return;
                }
                catch (Exception e) {
                    Debug.LogError($"ping error: \n{e}");
                }
            }
        }
    }
}