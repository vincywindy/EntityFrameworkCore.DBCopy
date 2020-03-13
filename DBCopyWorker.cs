using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
        readonly T _fromDb;
        readonly T _toDb;
        readonly DBCopyConfig _config;
        /// <summary>
        /// Inti CopyWorker
        /// </summary>
        /// <param name="FromDb">Which Db is Copy From</param>
        /// <param name="ToDb">Which Db is Copy To</param>
        /// <param name="config">The config of copy db</param>
        public DBCopyWorker(T FromDb, T ToDb, DBCopyConfig config = null)
        {
            _fromDb = FromDb;
            _toDb = ToDb;
            _config = config;
            CheckShadow(FromDb.Model.GetEntityTypes().Where(d => !d.IsAbstract()));
        }
        private void CheckShadow(IEnumerable<IEntityType> Entitys)
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
