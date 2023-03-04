using ET;
using System;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 现在还不能运行：是因为我的服务器没有起来，只有客户端。我还没弄明白 mac 上要如何运行这些老版本的 .exe 可执行程序
// 客户端逻辑：这个项目整个的是客户端逻辑【原项目主不要双端，不要热更新，不要服务器】，但我需要的是服务器逻辑，我仍然需要使用ET7 的头。就是把斗地主样例里的头【注册登录】，填充到 ET7 里去
public class Client : MonoBehaviour { // 【自己的】要两个按钮：＋注册，登录
    public string address = "127.0.0.1";
    public const int port = 10002;

    TextMeshProUGUI text;  // 给个提示信息：你没有帐房，请先注册；注册成功；登录失败等
    public Button button;
    public Button signUpbutton; // 注册 

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
        signUpbutton.onClick.AddListener(OnSignUpButtonClick);
        text.text = "Connect";
    }
    
    bool isConnected => session != null && !session.IsDisposed; // 会话框建立起来了，并且还没有回收

    private async void OnButtonClick() { // 异步方法 
        if (isConnected) { 
            text.text = "Connect";
            ping.text = "Ping: - ";
            session.Send(new C2M_Stop()); // 向服务器发消息: 停止消息  
            session.Dispose(); // 感觉这里就有点儿怪：不奇怪，连接好的状态，再点，就停止 
            session = null;
        } else {
            var host = $"{address}:{port}"; // 远程服务器的地址：Realm 注册登录服的 IP 地址 
            var result = await LoginAsync(host); // 登录远程服务器：自己的逻辑，这里是 Realm 注册登录服，先注册才登录
            text.text = result ? "Connected" : "Try again";
        }
    }
    // 注册成功后：按钮失活。去ET 框架里找下，注册与登录所用的 Session 需要回收吗？注册登录过程应该很短，可以不缓存
    // 现项目没有服务器端的注册登录逻辑。Realm 注册登录用，需要缓存保存用户登录，与个人帐户信息。【现在的基本要求，以后写熟悉了，可以加保持用户游戏数据】
    // 需要添加 MongoDB 数据库相关模块，但仍用 allServer 模式 
    private async void OnSignUpButtonClick() {
        signUpbutton.gameObject.SetActive(false); // 按钮失活
        // 创建一个ETModel层的Session
        R2C_Login r2CLogin;
        Session forRealm = NetKcpComponent.Create(NetworkHelper.ToIPEndPoint(address));
        r2CLogin = (R2C_Login)await forRealm.Call(new C2R_Login() { Account=username.text, Password=password.text });
        // forRealm?.Dispose(); // 延后处理：怎么处理到某个自动逻辑里去？
        Debug.Log($"{nameof(Client)}: ");
        
        
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
            session = NetKcpComponent.Create(NetworkHelper.ToIPEndPoint(r2CLogin.Address)); // 这里拿到的地址，应该是网关服的地址
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
