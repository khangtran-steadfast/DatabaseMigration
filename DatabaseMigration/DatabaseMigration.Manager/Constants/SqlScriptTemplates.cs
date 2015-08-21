using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager.Constants
{
    public class SqlScriptTemplates
    {
        public const string IDENTITY_INSERT_ON = "SET IDENTITY_INSERT {0} ON";
        public const string IDENTITY_INSERT_OFF = "SET IDENTITY_INSERT {0} OFF";

        public const string INSERT = @"INSERT {0} {1}";
        public const string INSERT_VALUES = @"VALUES {0}";
        public const string INSERT_SELECT_FROM = 
@"INSERT INTO {0} ({1})
SELECT {2}
FROM {3}";

        public const string TRUNCATE_TABLE = @"TRUNCATE TABLE {0}";

        public const string FIELD = "[{0}]";
        public const string NEW_FIELD = "[New_{0}]";

        // Insert distinct using SQL Merge (just insert when source PK <> target PK)
        public const string MERGE_HAS_PK =
@"MERGE {TargetTableFullName} AS t
USING(
     SELECT {SourceSelectFields}
     FROM {SourceTableFullName}
     ) AS s
ON ({RecordComparison})  
WHEN NOT MATCHED   
    THEN INSERT ({TargetInsertFields}) VALUES ({SourceInsertFields})
OUTPUT '{SourceTableName}', '{SourcePKName}', '{SourcePKDataType}', s.[{SourcePKName}], INSERTED.[{TargetPKName}] INTO [TempDatabase].dbo.TrackingRecords;";

        // Insert distinct using SQL Merge (just insert when source records <> target records)
        public const string MERGE_NO_PK =
@"MERGE {TargetTableFullName} AS t
USING(
     SELECT {SourceSelectFields}
     FROM {SourceTableFullName}
     ) AS s
ON ({RecordComparison})  
WHEN NOT MATCHED   
    THEN INSERT ({TargetInsertFields}) VALUES ({SourceInsertFields});";

        // Update circle references
        public const string MERGE_UPDATE_CIRCLE_REFERENCES =
@"MERGE {TargetTableFullName} AS t
USING(
     SELECT *
     FROM {SourceTableFullName} s JOIN [TempDatabase].dbo.[TrackingRecords] tr ON s.[{SourcePKName}] = tr.[PKOldValue]
     WHERE [TableName] = '{TargetTableName}'
     ) AS s
ON (t.[{TargetPKName}] = s.[PKNewValue])  
WHEN MATCHED   
    THEN UPDATE SET t.[{TargetFKName}] = (SELECT [PKNewValue] FROM [TempDatabase].dbo.[TrackingRecords] WHERE [TableName] = '{TargetReferenceTableName}' AND s.[{SourceFKName}] = [PKOldValue]);";

        public const string FIELD_COMPARE_EQUAL = @"t.[{TargetPKName}] = s.[{SourcePKName}]";
        public const string FIELD_COMPARE_LIKE = @"t.[{TargetPKName}] LIKE s.[{SourcePKName}]";

        public const string FK_SELECT = 
@"          [New_{SourceFKName}] = (SELECT [PKNewValue] FROM [TempDatabase].dbo.[TrackingRecords] WHERE [TableName] = '{FKTable}' AND [{SourceFKName}] = [PKOldValue])";

        public const string BLOB_SELECT =
@"          [New_{SourceBlobFieldName}] = (SELECT [BlobPointer] FROM [TempDatabase].dbo.[BlobPointers] WHERE [{SourcePKName}] = [PKValue])";
    }
}
