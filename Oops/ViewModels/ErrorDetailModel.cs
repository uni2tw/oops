using Oops.DataModels;
using System.Collections.Generic;

namespace Oops.ViewModels
{
    public class ErrorDetailModel : ErrorModel
    {
        public List<Error.DetailItem> serverValues { get; set; }

        public List<Error.DetailItem> cookies { get; set; }

        public List<Error.DetailItem> querys { get; set; }
        public List<Error.DetailItem> forms { get; set; }
        public List<Error.DetailItem> headers { get; set; }
        public string detail { get; set; }
    }

}
