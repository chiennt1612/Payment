using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.Dtos;

namespace Payment.Helpers
{
    public enum SearchType
    {
        Equal = 0,
        GreaterThanOrEqual = 1,
        GreaterThan = 2,
        LessThanOrEqual = 3,
        LessThan = 4,
        IsRegexp = 5
    }

    public enum FieldType
    {
        String = 0,
        Datetime = 1,
        //Date = 2,
        Numeric = 3
    }

    public enum SearchOperator
    {
        And = 1,
        Or = 0
    }

    public class ObjFilter
    {
        public ObjFilter()
        {
            _FieldName = "CreatedTime";
            _Type = FieldType.Datetime;
            _Value = DateTime.UtcNow.AddDays(-90).ToString("MM/dd/yyyy HH:mm:ss");
            _SearchType = SearchType.GreaterThanOrEqual;
            _SearchOperator = SearchOperator.And;
        }
        public string _FieldName { get; set; }
        public FieldType _Type { get; set; }
        public string _Value { get; set; }
        public SearchType _SearchType { get; set; }
        public SearchOperator _SearchOperator { get; set; }
    }

    public static class ElasticSearch
    {
        public static async Task<IEnumerable<TransactionDTO>> SeachTransactionAsync(this IElasticClient _elasticClient, List<ObjFilter> _Query)
        {
            SearchDescriptor<TransactionDTO> searchDescriptor = new SearchDescriptor<TransactionDTO>();
            QueryContainer andQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in _Query)
            {                
                switch(item._Type)
                {
                    case FieldType.Datetime:
                        #region Datetime
                        if (!string.IsNullOrEmpty(item._FieldName) && !string.IsNullOrEmpty(item._Value))
                        {
                            DateRangeQuery dq = new DateRangeQuery();
                            dq.Field = item._FieldName;
                            switch (item._SearchType)
                            {
                                case SearchType.Equal:
                                    dq.Equals(DateMath.Anchored(item._Value));
                                    break;
                                case SearchType.GreaterThanOrEqual:
                                    dq.GreaterThanOrEqualTo = DateMath.Anchored(item._Value);
                                    break;
                                case SearchType.GreaterThan:
                                    dq.GreaterThan = DateMath.Anchored(item._Value);
                                    break;
                                case SearchType.LessThan:
                                    dq.LessThan = DateMath.Anchored(item._Value);
                                    break;
                                case SearchType.LessThanOrEqual:
                                    dq.LessThanOrEqualTo = DateMath.Anchored(item._Value);
                                    break;
                            }
                            
                            dq.Format = default;

                            //if (andQuery == null)
                                andQuery = dq;
                            queryContainerList.Add(andQuery);
                            //else
                            //{
                            //    if (item._SearchOperator == SearchOperator.And)
                            //    {
                            //        andQuery = andQuery & dq;
                            //    }
                            //    else
                            //    {
                            //        andQuery = andQuery | dq;
                            //    }
                            //}
                        }
                        #endregion Datetime
                        break;
                    //case FieldType.Date:
                    //    break;
                    case FieldType.Numeric:
                        #region Numeric
                        if (!string.IsNullOrEmpty(item._FieldName) && !string.IsNullOrEmpty(item._Value))
                        {
                            NumericRangeQuery dq = new NumericRangeQuery();
                            dq.Field = item._FieldName;
                            switch (item._SearchType)
                            {
                                case SearchType.Equal:
                                    dq.Equals(double.Parse(item._Value));
                                    break;
                                case SearchType.GreaterThanOrEqual:
                                    dq.GreaterThanOrEqualTo = double.Parse(item._Value);
                                    break;
                                case SearchType.GreaterThan:
                                    dq.GreaterThan = double.Parse(item._Value);
                                    break;
                                case SearchType.LessThan:
                                    dq.LessThan = double.Parse(item._Value);
                                    break;
                                case SearchType.LessThanOrEqual:
                                    dq.LessThanOrEqualTo = double.Parse(item._Value);
                                    break;
                            }

                            //if (andQuery == null)
                            andQuery = dq;
                            queryContainerList.Add(andQuery);
                            //else
                            //{
                            //    if (item._SearchOperator == SearchOperator.And)
                            //    {
                            //        andQuery = andQuery & dq;
                            //    }
                            //    else
                            //    {
                            //        andQuery = andQuery | dq;
                            //    }
                            //}
                        }
                        #endregion Numeric
                        break;
                    default:
                        #region keyword
                        if (!string.IsNullOrEmpty(item._FieldName) && !string.IsNullOrEmpty(item._Value))
                        {
                            TermQuery dq = new TermQuery
                            {
                                Field = item._FieldName,
                                Value = item._Value
                            };

                            //if (andQuery == null)
                            andQuery = dq;
                            queryContainerList.Add(andQuery);
                            //else
                            //{
                            //    if (item._SearchOperator == SearchOperator.And)
                            //    {
                            //        andQuery = andQuery & dq;
                            //    }
                            //    else
                            //    {
                            //        andQuery = andQuery | dq;
                            //    }
                            //}
                        }
                        #endregion keyword
                        break;
                }                
            }

            var reqs = (IEnumerable<TransactionDTO>)null;

            if (andQuery != null)
            {
                searchDescriptor
                    .Query(q => q.Bool(qb => qb.Should(queryContainerList.ToArray())));                
            }
            else
            {
                searchDescriptor
                        .Query(m => m.MatchAll());
            }
            reqs = (await _elasticClient.SearchAsync<TransactionDTO>(searchDescriptor)).Documents;
            return reqs;
        }
    }
}
