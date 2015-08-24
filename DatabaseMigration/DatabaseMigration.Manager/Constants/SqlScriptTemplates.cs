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

        /// <summary>
        /// Insert distinct using SQL Merge (just insert when source PK <> target PK)
        /// Need to create temp table to output tracking records first and then insert to real tracking records table because
        /// of some strange error when output directly to real table
        /// </summary>
        public const string MERGE_HAS_PK =
@"CREATE TABLE #Temp
(
	TableName nvarchar(50),
	PKName nvarchar(50),
	PKType nvarchar(50),
	PKOldValue nvarchar(50),
    PKNewValue nvarchar(50), 
)

MERGE {TargetTableFullName} AS t
USING(
     SELECT {SourceSelectFields}
     FROM {SourceTableFullName}
     {SourceConditions}
     ) AS s
ON ({RecordComparison})  
WHEN NOT MATCHED   
    THEN INSERT ({TargetInsertFields}) VALUES ({SourceInsertFields})
OUTPUT '{SourceTableName}', '{SourcePKName}', '{SourcePKDataType}', s.[{SourcePKName}], INSERTED.[{TargetPKName}] INTO #Temp;

INSERT INTO [TempDatabase].dbo.TrackingRecords
SELECT * FROM #Temp

DROP TABLE #Temp";

        // Insert distinct using SQL Merge (just insert when source records <> target records)
        public const string MERGE_NO_PK =
@"MERGE {TargetTableFullName} AS t
USING(
     SELECT {SourceSelectFields}
     FROM {SourceTableFullName}
     {SourceConditions}
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
        public const string FIELD_NOT_NULL = @"{0} <> NULL";

        public const string FK_SELECT = 
@"          [New_{SourceFKName}] = (SELECT [PKNewValue] FROM [TempDatabase].dbo.[TrackingRecords] WHERE [TableName] = '{FKTable}' AND [{SourceFKName}] = [PKOldValue])";

        public const string BLOB_SELECT =
@"          [New_{SourceBlobFieldName}] = (SELECT [BlobPointer] FROM [TempDatabase].dbo.[BlobPointers] WHERE '{SourceBlobFieldName}' = [BlobFieldName] AND [{SourcePKName}] = [PKValue])";
    }
}
