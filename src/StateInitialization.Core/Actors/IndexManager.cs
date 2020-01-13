using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class IndexManager
    {
        private static readonly object IndexLock = new object();
        private readonly SqlConnection _sqlConnection;

        public IndexManager(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public void DisableIndexes(TableName table)
        {
            DisableIndexes(GetTable(table));
        }

        public void EnableIndexes(TableName table)
        {
            EnableIndexes(GetTable(table));
        }

        public void CopyIndexes(Table sourceTable, Table targetTable)
        {
            lock (IndexLock)
            {
                foreach (Index index in sourceTable.Indexes)
                {
                    try
                    {
                        var newIndex = CreateIndexCopy(targetTable, index);
                        newIndex.Create();
                    }
                    catch (Exception ex)
                    {
                        throw new DataException($"Error occured while creating copy of index {index.Name} from table {sourceTable.Name} in table {targetTable.Name}", ex);
                    }
                }
            }
        }

        private Index CreateIndexCopy(Table targetTable, Index index)
        {
            var newIndex = new Index(targetTable, Guid.NewGuid().ToString("N"))
            {
                BoundingBoxXMax = index.BoundingBoxXMax,
                BoundingBoxXMin = index.BoundingBoxXMin,
                BoundingBoxYMax = index.BoundingBoxYMax,
                BoundingBoxYMin = index.BoundingBoxYMin,
                BucketCount = index.BucketCount,
                CellsPerObject = index.CellsPerObject,
                CompactLargeObjects = index.CompactLargeObjects,
                DisallowPageLocks = index.DisallowPageLocks,
                DisallowRowLocks = index.DisallowRowLocks,
                FileGroup = index.FileGroup,
                FileStreamFileGroup = index.FileStreamFileGroup,
                FileStreamPartitionScheme = index.FileStreamPartitionScheme,
                FillFactor = index.FillFactor,
                FilterDefinition = index.FilterDefinition,
                IgnoreDuplicateKeys = index.IgnoreDuplicateKeys,
                IndexKeyType = index.IndexKeyType,
                IndexType = index.IndexType,
                IndexedXmlPathName = index.IndexedXmlPathName,
                IsClustered = index.IsClustered,
                IsFullTextKey = index.IsFullTextKey,
                IsMemoryOptimized = index.IsMemoryOptimized,
                IsUnique = index.IsUnique,
                Level1Grid = index.Level1Grid,
                Level2Grid = index.Level2Grid,
                Level3Grid = index.Level3Grid,
                Level4Grid = index.Level4Grid,
                LowPriorityAbortAfterWait = index.LowPriorityAbortAfterWait,
                LowPriorityMaxDuration = index.LowPriorityMaxDuration,
                MaximumDegreeOfParallelism = index.MaximumDegreeOfParallelism,
                NoAutomaticRecomputation = index.NoAutomaticRecomputation,
                OnlineIndexOperation = index.OnlineIndexOperation,
                PadIndex = index.PadIndex,
                ParentXmlIndex = index.ParentXmlIndex,
                PartitionScheme = index.PartitionScheme,
                SecondaryXmlIndexType = index.SecondaryXmlIndexType,
                SortInTempdb = index.SortInTempdb,
                SpatialIndexType = index.SpatialIndexType
            };

            newIndex.ExtendedProperties.Add(new ExtendedProperty(newIndex, "CopyOf", index.Name));

            foreach (IndexedColumn column in index.IndexedColumns)
            {
                var newColumn = new IndexedColumn(newIndex, column.Name, column.Descending)
                {
                    IsIncluded = column.IsIncluded
                };

                newIndex.IndexedColumns.Add(newColumn);
            }

            return newIndex;
        }

        private Table GetTable(TableName table)
        {
            var database = _sqlConnection.GetDatabase();
            var tableInDb = database.GetTable(table);

            if (tableInDb == null)
            {
                throw new ArgumentException($"Table {table} was not found", nameof(table));
            }

            return tableInDb;
        }

        private void DisableIndexes(Table tableType)
        {
            lock (IndexLock)
            {
                foreach (var index in tableType.Indexes.Cast<Index>().Where(x => x != null && !x.IsClustered && !x.IsDisabled))
                {
                    index.Alter(IndexOperation.Disable);
                }
            }
        }

        private void EnableIndexes(Table tableType)
        {
            lock (IndexLock)
            {
                tableType.EnableAllIndexes(IndexEnableAction.Rebuild);
            }
        }
    }
}