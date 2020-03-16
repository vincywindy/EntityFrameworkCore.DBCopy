
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EntityFrameworkCore
{
    public class DBCopyConfig
    {
        public DBCopyConfig()
        {
            WorkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbCopyTemp");

        }
        /// <summary>
        /// The Logger of Microsoft
        /// </summary>
        public Microsoft.Extensions.Logging.ILogger<DBCopyConfig> MLogger { get; set; }
        /// <summary>
        /// The Logger of Serilog
        /// </summary>
        public Serilog.ILogger SLogger { get; set; }
        /// <summary>
        /// Work need a directory to cache some data.Default is AppDomain.CurrentDomain.BaseDirectory/DbCopyTemp
        /// </summary>
        public string WorkPath { get; set; }
        /// <summary>
        /// Let's "Shadow properties must be explicitly defined" do not be an error,Default is false
        /// </summary>
        public bool IgnoreShadowPropery { get; set; } = false;
        /// <summary>
        /// The count of the process
        /// </summary>
        public int ProcessCount { get; set; } = 50000;
    }
}
