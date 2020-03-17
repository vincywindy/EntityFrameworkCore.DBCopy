using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Z.BulkOperations;
using Z.EntityFramework.Extensions;

namespace EntityFrameworkCore
{
    /// <summary>
    /// The CopyWorker
    /// </summary>
    /// <summary xml:lang="zh">
    /// 用来启动数据库复制的工具人
    /// </summary>
    public class DBCopyWorker<T> where T : DbContext
    {
        readonly DbContextOptions<T> _fromDbConfig;
        readonly DbContextOptions<T> _toDbConfig;
        readonly DBCopyConfig _config;
        readonly List<IEntityType> _entityTypes;
        readonly Type _listtype;
        readonly MethodInfo _bulkinsert = typeof(DbContextExtensions).GetMethods().Where(d => d.Name == "BulkInsert" && d.GetParameters().Length == 3).First();
        readonly Action<BulkOperation> act = new Action<BulkOperation>((d) => { d.InsertKeepIdentity = true; });
        /// <summary>
        /// Inti CopyWorker
        /// </summary>
        /// <param name="FromDb">Which Db is Copy From</param>
        /// <param name="ToDb">Which Db is Copy To</param>
        /// <param name="config">The config of copy db</param>
        public DBCopyWorker(DbContextOptions<T> FromDb, DbContextOptions<T> ToDb, DBCopyConfig config = null)
        {
            if (EntityFrameworkManager.ContextFactory == null)
            {
                EntityFrameworkManager.ContextFactory = context =>
                {
                    return (T)Activator.CreateInstance(typeof(T), ToDb);
                };
            }

            _listtype = typeof(List<>);
            _fromDbConfig = FromDb;
            _toDbConfig = ToDb;
            _config = config;
            using (var context = (T)Activator.CreateInstance(typeof(T), FromDb))
            {
                _entityTypes = context.Model.GetEntityTypes().Where(d => !d.IsAbstract()).ToList();
                CheckShadow(_entityTypes);
            }


        }
        /// <summary>
        /// Loaded DataTotal
        /// </summary>
        public long LoadCount { get; private set; }
        /// <summary>
        /// Inserted DataTotal
        /// </summary>
        public long InsertCount { get; private set; }
        /// <summary>
        /// Export the data then import
        /// </summary>
        /// <returns></returns>
        public void Copy()
        {
            Export();
            Import();

        }
        /// <summary>
        /// Import the data to "To" Context
        /// </summary>
        /// <returns></returns>
        public void Import()
        {

            using (var _toDb = (T)Activator.CreateInstance(typeof(T), _toDbConfig))
            {
                var Finishdic = Path.Combine(_config.WorkPath, "Finish");
                if (!Directory.Exists(Finishdic))
                {
                    Directory.CreateDirectory(Finishdic);
                }
                foreach (var t in _entityTypes)
                {
                    var tablename = t.GetTableName();
                    var dbname = t.DisplayName();
                    var jsonpath = Path.Combine(_config.WorkPath, dbname + ".json");
                    if (!File.Exists(jsonpath))
                    {
                        Info($"{jsonpath} is no exists,skip to import");
                        continue;
                    }
                    var dbInserttype = _listtype.MakeGenericType(t.ClrType);//list<T>

                    var entitylist = JsonHelper.GetJson(jsonpath, dbInserttype);
                    IList list = (IList)Activator.CreateInstance(dbInserttype);
                    foreach (var en in entitylist)
                    {
                        list.Add(en);
                        InsertCount++;
                        if (list.Count >= _config.ProcessCount)
                        {
                            BulkInsert(_toDb, t, tablename, dbInserttype, list);
                            list.Clear();
                            Info($"Inserted {tablename} count:{InsertCount}");
                        }

                    }
                    if (list.Count > 0)
                    {
                        BulkInsert(_toDb, t, tablename, dbInserttype, list);
                        list.Clear();
                        Info($"Inserted {tablename} count:{InsertCount}");
                    }
                    var finjsonpath = Path.Combine(Finishdic, dbname + ".json");
                    if (File.Exists(finjsonpath))
                    {
                        File.Delete(finjsonpath);
                    }
                    File.Move(jsonpath, finjsonpath);
                    Info($"Inserted {tablename} Count:{InsertCount}");


                }
            }
        }
        private void GetQuestioninfo(T _toDb, IEntityType t, Type dbInserttype, IList list)
        {
            Info($"Check Error list {list.Count}");

            if (list.Count == 1)
            {
                Error(JsonConvert.SerializeObject(list[0]));
            }
            var halflist = (IList)Activator.CreateInstance(dbInserttype);
            foreach (var ls in list)
            {

                halflist.Add(ls);
                if (halflist.Count >= list.Count / 2)
                {
                    try
                    {
                        _bulkinsert.MakeGenericMethod(t.ClrType).Invoke(_toDb, new object[] { _toDb, halflist, act });
                        halflist.Clear();
                    }
                    catch
                    {
                        GetQuestioninfo(_toDb, t, dbInserttype, halflist);
                    }
                }
            }
            if (halflist.Count > 0)
            {
                try
                {
                    _bulkinsert.MakeGenericMethod(t.ClrType).Invoke(_toDb, new object[] { _toDb, halflist, act });
                    halflist.Clear();
                }
                catch
                {
                    GetQuestioninfo(_toDb, t, dbInserttype, halflist);
                }
            }

        }
        public virtual void BulkInsert(T _toDb, IEntityType t, string tablename, Type dbInserttype, IList list)
        {
            var listcopy = (IList)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(list), dbInserttype);


            try
            {
                _bulkinsert.MakeGenericMethod(t.ClrType).Invoke(_toDb, new object[] { _toDb, list, act });
         
            }
            catch (Exception ex)
            {
                GetQuestioninfo(_toDb, t, dbInserttype, list);
                throw ex;
            }
            var updatelist = Check(list, listcopy, dbInserttype);
            if (updatelist.Count > 0)
            {
                //because https://github.com/zzzprojects/EntityFramework-Extensions/issues/173
                Info($"Update {tablename} data:{updatelist.Count}");
                var method2 = typeof(DbContextExtensions).GetMethods().Where(d => d.Name == "BulkUpdate" && d.GetParameters().Length == 3).First();
                method2.MakeGenericMethod(t.ClrType).Invoke(_toDb, new object[] { _toDb, updatelist, act });
            }
        }

        static IList Check(IList before, IList after, Type t)
        {

            var list = (IList)Activator.CreateInstance(t);
            if (before.Count == 0)
                return list;
            var props = before[0].GetType().GetProperties();

            for (var i = 0; i < before.Count; i++)
            {
                var a = before[i];
                var b = after[i];
                foreach (var p in props)
                {
                    var aval = p.GetValue(a);
                    var bval = p.GetValue(b);
                    if (aval?.ToString() != bval?.ToString())
                    {
                        list.Add(a);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Export the data from "From" Context
        /// </summary>
        /// <returns></returns>
        public void Export()
        {
            using (var _fromDb = (T)Activator.CreateInstance(typeof(T), _fromDbConfig))
            {
                foreach (var t in _entityTypes)
                {
                    var tablename = t.GetTableName();
                    var dbname = t.DisplayName();
                    var jsonpath = Path.Combine(_config.WorkPath, dbname + ".json");
                    if (File.Exists(jsonpath))
                    {
                        //already had,skip
                        Info($"{jsonpath} already existed,skip to load");
                        continue;
                    }
                    var entitylist = new List<object>(_config.ProcessCount);
                    var count = 0;
                    using (var ls = _fromDb.Set(t.ClrType).AsNoTracking().GetEnumerator())
                    {

                        Info($"Get data from {dbname}");
                        while (ls.MoveNext())
                        {
                            entitylist.Add(ls.Current);

                            LoadCount++;
                            count++;
                            if (entitylist.Count >= _config.ProcessCount)
                            {
                                JsonHelper.AddJson(jsonpath, entitylist);
                                entitylist.Clear();
                            }

                        }
                        if (entitylist.Count > 0)
                        {
                            JsonHelper.AddJson(jsonpath, entitylist);
                            entitylist.Clear();
                        }

                    }
                    Info($"Loaded {tablename},total {count},save to {jsonpath}");

                }
            }
        }
        private void CheckShadow(List<IEntityType> Entitys)
        {
            var shadows = Entitys.SelectMany(d => d.GetProperties().Select(dd => new { prop = dd, entity = d })).Where(d => d.prop.IsShadowProperty()).Select(d => d.entity.DisplayName() + "." + d.prop.Name);
            if (shadows.Count() > 0)
            {
                if (!_config.IgnoreShadowPropery)
                    Error($"Shadow properties must be explicitly defined \n{string.Join("\n", shadows)}");
                else
                    Warn($"Shadow properties maybe explicitly defined \n{string.Join("\n", shadows)}");
            }
        }
        #region Log
        private void Warn(string msg)
        {
            _config.MLogger?.LogWarning(msg);
            _config.SLogger?.Warning(msg);
        }
        private void Info(string msg)
        {
            _config.MLogger?.LogInformation(msg);
            _config.SLogger?.Information(msg);
        }
        private void Error(string msg)
        {
            _config.MLogger?.LogError(msg);
            _config.SLogger?.Error(msg);
            throw new Exception(msg);
        }
        #endregion
    }
}
