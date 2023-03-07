using System;
using UnityEngine;
using static ET.TimerManager;
namespace ET {
    public class Ping { // 这是，心跳包吗？心跳包单独独立，为了消息模块的解偶。这个单独会有依赖的模块，怎么依赖的（消息内容？），把这个模块看懂【之前还没看过】
        public C2G_Ping C2G_Ping = new C2G_Ping(); // 为什么会感觉，这个消息是空的呢？

        public long Id { get; set; }
        public long InstanceId { get; private set; }
        public static TimeInfo TimeInfo => TimeInfo.Instance;
        public Action<long> OnPingRecalculated; // 比较重要： 
        public bool IsDisposed => InstanceId == 0;
        public long delay; // 延迟值
        Session session;

        public Ping(Session session) { // 就是说，用这个会话框，来发送来往在线消息 
            InstanceId = IdGenerater.Instance.GenerateInstanceId(); // 这个组件创建时，会生成一个实例 id
            this.session = session; // 因为返回在线消息，也是需要一个会话框的
            _ = PingAsync(); // 这明白： _ 这个符号是什么意思？总之，做的事就是，每隔2 秒，周期性地给服务器发消息，保持上线在线状态？
        } 
        public void Dispose() {
            if (this.IsDisposed) {
                return;
            }
            InstanceId = 0;
        }
        private async ETTask PingAsync() { // 异步任务：创建了，就开始无限运行
            long instanceId = InstanceId;  // 无限循环前，读到的这个值
            while (true) { // 这个是，无限循环的
                if (InstanceId != instanceId) { // 因为是无限循环，过程中出现任何的意外，仍然想不明白，什么情况下会出现这种情况
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
                    OnPingRecalculated?.Invoke(delay); // 这里说的意思是，等待这么久以后，再触发回调？
                    TimeInfo.ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2; // 算的是：服务器，与这个当前客户端的时差，用于两端同步？
                    Debug.Log($"{nameof(Ping)}:  ping = {delay} - {response.Time} - {response.Message} - {TimeInfo.ServerFrameTime()}");
                    await WaitAsync(2000); 
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