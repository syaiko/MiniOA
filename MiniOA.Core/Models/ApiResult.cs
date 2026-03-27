using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniOA.Core.Models
{
    public class ApiResult<T>
    {
        public int Code { get; set; } = 200; //状态码
        public string Message { get; set; } = "Success";
        public T? Data { get; set; } //响应内容
        public bool Success { get; set; } = true;

        public static ApiResult<T> Ok(T data, string msg = "操作成功")
            => new() { Data = data, Message = msg , Success = true };

        public static ApiResult<T> Fail(string msg, int code = 500)
            => new() { Code = code, Message = msg, Success = false };

    }

    // 分页查询参数
    public class PaginationQuery
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
    }

    // 分页响应
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}

