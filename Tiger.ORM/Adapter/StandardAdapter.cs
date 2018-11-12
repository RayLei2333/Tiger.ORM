﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Dapper;
using Tiger.ORM.ModelConfiguration;
using System.Linq;
using Tiger.ORM.Utilities;

namespace Tiger.ORM.Adapter
{
    public abstract class StandardAdapter : ISqlAdapter
    {
        //占位符 类似于sqlserver中的关键词需要“[column]”
        protected string LeftPlaceholder { get; set; }
        protected string RightPlaceholder { get; set; }




        #region Insert sql
        public virtual string Insert(object entity, out DynamicParameters parameters)
        {
            Check.NotNull(entity, "insert entity");
            string sqlTmpl = "INSERT INTO {0}({1}) VALUE ({2})";
            parameters = new DynamicParameters();
            //INSERT INTO table(C1,C2,C3,C4,C5) VALUES (@C1,@C2,@C3,@C4,@C5)
            Type entityType = entity.GetType();
            string tableName = Relations.GetTableName(entityType);

            IEnumerable<PropertyInfo> columns = Relations.GetColumns(entityType);

            StringBuilder columnStr = new StringBuilder(),
                          columnParaStr = new StringBuilder();

            //append key
            this.AppendInsertKey(entity, entityType, columns.Count(), columnStr, columnParaStr, parameters);

            //append column
            this.AppendInsertColumn(entity, columns, columnStr, columnParaStr, parameters);

            //get can execute sql.
            string sql = string.Format(sqlTmpl, tableName, columnStr.ToString(), columnParaStr.ToString());

            return sql;
        }

        private void AppendInsertKey(object entity, Type entityType, int columnCount, StringBuilder columnStr, StringBuilder columnParaStr, DynamicParameters parameters)
        {
            PropertyInfo key = Relations.GetKey(entityType);
            string keyname = Relations.GetKeyName(key, out KeyAttribute keyAttribute);
            //append key
            if (keyAttribute.KeyType == KeyType.GUID || keyAttribute.KeyType == KeyType.AutoGUID)
            {
                columnStr.Append($@"{LeftPlaceholder}{keyname}{RightPlaceholder}");
                columnParaStr.Append($"@{keyname}");
                if (columnCount > 0)
                {
                    columnStr.Append(",");
                    columnParaStr.Append(",");
                }
                if (keyAttribute.KeyType == KeyType.GUID)
                    parameters.Add($"@{keyname}", key.GetValue(entity));
                else
                    parameters.Add($"@{keyname}", Guid.NewGuid().ToString("d"));
            }
        }

        private void AppendInsertColumn(object entity, IEnumerable<PropertyInfo> columns, StringBuilder columnStr, StringBuilder columnParaStr, DynamicParameters parameters)
        {
            int count = columns.Count() - 1,
                index = 0;

            //append column
            foreach (var item in columns)
            {
                string columnName = Relations.GetColumnName(item);
                //ColumnAttribute columnAttr = item.GetCustomAttribute<ColumnAttribute>();
                //if (columnAttr == null || string.IsNullOrEmpty(columnAttr.Name))
                //    columnName = item.Name;
                //else
                //    columnName = columnAttr.Name;
                columnStr.Append($@"{LeftPlaceholder}{columnName}{RightPlaceholder}");
                columnParaStr.Append($"@{columnName}");
                parameters.Add($"@{columnName}", item.GetValue(entity));
                if (index < count)
                {
                    columnStr.Append(",");
                    columnParaStr.Append(",");
                }
                index++;
            }
        }
        #endregion

        public virtual string Update(object entity, out DynamicParameters parameters)
        {
            //SQL : UPDATE table SET column1=@column1,column2=@column2 where key = @key;
            string sqlTmpl = "UPDATE {0} SET {1} WHERE {2}";
            Type entityType = entity.GetType();
            parameters = new DynamicParameters();
            string tableName = Relations.GetTableName(entityType);
            IEnumerable<PropertyInfo> columns = Relations.GetColumns(entityType);
            PropertyInfo key = Relations.GetKey(entityType);
            string keyname = Relations.GetKeyName(key, out KeyAttribute keyAttribute);

            StringBuilder columnStr = new StringBuilder(),
                          keyStr = new StringBuilder();
            keyStr.Append($"{LeftPlaceholder}{keyname}{RightPlaceholder}=@{keyname}");
            parameters.Add($"@{keyname}", key.GetValue(entity));

            int index = 0,
                columnCount = columns.Count() - 1;
            foreach (var item in columns)
            {
                string columnName = Relations.GetColumnName(item),
                       paraName = $"@{columnName}";
                columnStr.Append($"{LeftPlaceholder}{columnName}{RightPlaceholder}={paraName}");
                parameters.Add(paraName, item.GetValue(entity));
                if (index < columnCount)
                    columnStr.Append(",");
                index++;
            }
            string sql = string.Format(sqlTmpl, tableName, columnStr.ToString(), keyStr.ToString());
            return sql;
        }

        #region Delete
        public virtual string Delete<T>(object key, out DynamicParameters parameters)
        {
            Check.NotNull(key,"key");
            Type entityType = typeof(T);
            parameters = new DynamicParameters();
            string sql = this.Delete(entityType, null, key, parameters);
            return sql;
        }

        public virtual string Delete(object entity, out DynamicParameters parameters)
        {
            Check.NotNull(entity, "delete entity");
            Type entityType = entity.GetType();
            parameters = new DynamicParameters();
            string sql = this.Delete(entityType, entity, null, parameters);
            return sql;
        }

        private string Delete(Type entityType, object entity, object value, DynamicParameters parameters)
        {
            string tableName = Relations.GetTableName(entityType);
            PropertyInfo property = Relations.GetKey(entityType);
            string keyName = Relations.GetKeyName(property, out KeyAttribute keyAttribute);
            string sql = $"DELETE FROM {tableName} WHERE {LeftPlaceholder}{keyName}{RightPlaceholder}=@{keyName}";
            if (entity == null)
                parameters.Add($"@{keyName}", value);
            else
                parameters.Add($"@{keyName}", property.GetValue(entity));
            return sql;
        }
        #endregion

    }
}