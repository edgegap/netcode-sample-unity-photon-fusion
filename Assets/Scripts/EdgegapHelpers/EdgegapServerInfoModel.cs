
/// <summary>
/// Information about the edgegap server to transfer to
/// </summary>
public class EdgegapServerInfoModel
{
    public enum ServerStatus
    {
        Running = 0,
        SpinningUp = 1,
        Canceled = 2,
    }

    public int Port;
    public string IPAddress;
    public string LocationDesc;
    public ServerStatus Status;


    /*
    public void DeSerialize(BFSSerializer a_Serializer)
    {
        Port = a_Serializer.DeserializeInt();
        IPAddress = a_Serializer.DeserializeString();
        LocationDesc = a_Serializer.DeserializeString();
        if (a_Serializer.NumBytesRemainingToRead > 0)
        {
            Status = (ServerStatus)a_Serializer.DeserializeInt();
        }
    }

    public void Serialize(BFSSerializer a_Serializer)
    {
        a_Serializer.Serialize(Port);
        a_Serializer.Serialize(IPAddress);
        a_Serializer.Serialize(LocationDesc);
        a_Serializer.Serialize((int)Status);
    }
    */

    public static readonly EdgegapServerInfoModel SpinningUp = new EdgegapServerInfoModel()
    {
        Status = ServerStatus.SpinningUp
    };

    public static readonly EdgegapServerInfoModel Canceled = new EdgegapServerInfoModel()
    {
        Status = ServerStatus.Canceled
    };
}