using System;
using System.Collections.Generic;
using System.Text;

namespace Work_Service.Application.Abstractions
{
    public class ResponseObject<T> 
    {
        public ResponseObject(int statusCode,bool Error,T? Response,string? ErrorMessage){
            StatusCode= statusCode;
            this.Error= Error;
            this.ErrorMessage= ErrorMessage;
            this.data = Response;
        }
        public int StatusCode { get; set; }
        public bool Error { get; set; }
        public T? data { get; set; }
        public string? ErrorMessage { get; set; }
    }
    public class ResponseObject
    {
        public ResponseObject(int statusCode, bool Error, string? ErrorMessage)
        {
            StatusCode = statusCode;
            this.Error = Error;
            this.ErrorMessage = ErrorMessage;
            
        }
        public int StatusCode { get; set; }
        public bool Error { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
