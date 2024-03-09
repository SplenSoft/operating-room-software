using Unity.Services.CloudCode.Core;
using Newtonsoft.Json;

namespace SplenSoft.OperatingRoomSoftware.Net;

public class Database
{
    [CloudCodeFunction("DoMetaDataOperation")]
    public string DoMetaDataOperation(string json)
    {
        MetaDataOperationResult result = new MetaDataOperationResult();

        try
        {
            string connection = "postgresql://ors:cOdTQc-RIUn3v_vCu8wDug@brassy-growler-13696.7tt.aws-us-east-1.cockroachlabs.cloud:26257/defaultdb?sslmode=verify-full;";

            using (OrsDbContext ctx = new OrsDbContext(connection))
            {
                // do your EF magic here.....
            }


            MetaDataOperation operation = JsonConvert.DeserializeObject<MetaDataOperation>(json) ??
                throw new Exception($"MetaData operation json data was null");

            //switch (operation.OperationType) 
            //{
            //    case MetaDataOperationType.GetLastModified:

            //}
            result.ResultType = MetaDataOpertaionResultType.Success;
            result.Message = "Test completed successfully";
        }
        catch (Exception ex) 
        {
            result.ResultType = MetaDataOpertaionResultType.Exception;
            result.Message = ex.Message;
        }

        return JsonConvert.SerializeObject(result);
    }
}

public class StoredMetaData
{
    public int ID { get; set; }
    public long LastModified { get; set; }
    public string AssetBundleName { get; set; }
    public string SelectableMetaData { get; set; }
}

[Serializable]
public class MetaDataOperationResult
{
    public MetaDataOperationType OperationType { get; set; }
    public MetaDataOpertaionResultType ResultType { get; set; }
    public string Message { get; set; }
}

[Serializable]
public class MetaDataOperation
{
    public MetaDataOperationType OperationType { get; set; }
    public long LastModified { get; set; }
    public string AssetBundleName { get; set; }
    public string SelectableMetaData { get; set; }
}

public enum MetaDataOpertaionResultType
{
    None,
    Success,
    Error,
    Exception
}

public enum MetaDataOperationType
{
    Get,
    Set,
    GetLastModified
}