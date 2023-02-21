using ET;
using System;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 现在还不能运行：是因为我的服务器没有起来，只有客户端。我还没弄明白 mac 上要如何运行这些老版本的 .exe 可执行程序
public class Client : MonoBehaviour {
    public Button button;
    public string address = "127.0.0.1";
    public const int port = 10002;
    TextMeshProUGUI text;

    NetKcpComponent NetKcpComponent;
    Session session;

    public TMP_InputField username;
    public TMP_InputField password;
    public TextMeshProUGUI ping;

    private void Awake() {
        username.text = name;
        text = button.GetComponentInChildren<TextMeshProUGUI>();
        NetKcpComponent = GetComponent<NetKcpComponent>(); // 它身上持了这个组件，应该是。可验证一下
    }
    private void Start() {
        button.onClick.AddListener(OnButtonClick);
        text.text = "Connect";
    }
    
    bool isConnected => session != null && !session.IsDisposed; // 会话框建立起来了，并且还没有回收

    private async void OnButtonClick() { // 异步方法 
        if (isConnected) {
            text.text = "Connect";
            ping.text = "Ping: - ";
            session.Send(new C2M_Stop()); // 向服务器发消息 
            session.Dispose(); // 感觉这里就有点儿怪
            session = null;
        } else {
            var host = $"{address}:{port}";
            var result = await LoginAsync(host);
            text.text = result ? "Connected" : "Try again";
        }
    }
    public async ETTask<bool> LoginAsync(string address) {
        bool isconnected = true;
        try {
            // 创建一个ETModel层的Session
            R2C_Login r2CLogin;
            Session forgate = null;
            forgate = NetKcpComponent.Create(NetworkHelper.ToIPEndPoint(address));
            
            r2CLogin = (R2C_Login)await forgate.Call(new C2R_Login() { Account=username.text, Password=password.text });
            forgate?.Dispose();
            Debug.Log($"{nameof(Client)}: ");
            // 创建一个gate Session,并且保存到SessionComponent中
            session = NetKcpComponent.Create(NetworkHelper.ToIPEndPoint(r2CLogin.Address));
            session.ping = new ET.Ping(session);
            session.ping.OnPingRecalculated += (delay) => { ping.text = $"Ping: {delay}"; };
            G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await session.Call(new C2G_LoginGate() { Key = r2CLogin.Key, GateId = r2CLogin.GateId });
            Debug.Log("登陆gate成功!");
            // 登录 map 服务器
            // 进入地图
            var request = new C2G_EnterMap() ;
            G2C_EnterMap map = await session.Call(request) as G2C_EnterMap;
            Debug.Log($"进入地图成功：  Net_id = {map.MyId}");
        }
        catch (Exception e) {
            isconnected = false;
            Debug.LogError($"登陆失败 - {e}");
        }
        return isconnected;
    }
}
