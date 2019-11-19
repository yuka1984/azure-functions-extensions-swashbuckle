using System;
using System.ComponentModel.DataAnnotations;

namespace TestModels
{
    /// <summary>
    /// テストモデル
    /// </summary>
    public class TestModel
    {
        /// <summary>
        /// Id
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 名前
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string Name { get; set; }

        /// <summary>
        /// 詳細説明
        /// </summary>
        [MaxLength(10240)]
        public string Description { get; set; }
    }
}
