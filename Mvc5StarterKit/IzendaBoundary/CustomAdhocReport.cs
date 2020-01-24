using Izenda.BI.Framework.CustomConfiguration;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Web;
using Izenda.BI.Framework.Models;
using Izenda.BI.Framework.Models.Common;
using Izenda.BI.Framework.Models.ReportDesigner;
using Izenda.BI.Framework.Utility;
using Izenda.BI.Framework.Models.Contexts;

namespace Mvc5StarterKit.IzendaBoundary
{
    /// <summary>
    /// This is the Custom AdhocExtension, it will override the lookup filter values, hidden filter, ....
    /// </summary>
    [Export(typeof(IAdHocExtension))]
    public class CustomAdhocReport : DefaultAdHocExtension
    {
        /// <summary>
        /// Override lookup filter data.
        /// 
        /// Requirement summary:
        /// If logged user has role VP --> show all region
        /// If logged user has role Manager --> show some regions ("South America", "North America")
        /// If logged user has role Employee --> show only one region ("South America")
        /// </summary>
        /// <param name="filterField"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public override List<string> OnPostLoadFilterData(ReportFilterField filterField, List<string> data)
        {
            // override dropdown value based on user role for filter on view "OrderDetailsByRegion" and field "CountryRegionName"
            if (filterField.SourceDataObjectName == "OrderDetailsByRegion" && filterField.SourceFieldName == "CountryRegionName"
                && (HttpContext.Current.User.IsInRole("Manager") || HttpContext.Current.User.IsInRole("Employee")))
            {
                ///Clear existing value
                data.Clear();

                // override dropdown's value based on User role

                //Manager, dropdown show "South America" and "North America"
                if (HttpContext.Current.User.IsInRole("Manager"))
                {
                    data.Add("South America");
                    data.Add("North America");
                }

                // Employee, dropdown show "South America" only.
                if (HttpContext.Current.User.IsInRole("Employee"))
                {
                    data.Add("South America");
                }
            }
            return base.OnPostLoadFilterData(filterField, data);
        }

        /// <summary>
        /// Override lookup filter data tree.
        /// 
        /// Requirement summary:
        /// If logged user has role VP --> show all region
        /// If logged user has role Manager --> show some regions ("South America", "North America")
        /// If logged user has role Employy --> show only one region ("South America")
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        public override List<ValueTreeNode> OnLoadFilterDataTree(QuerySourceFieldInfo fieldInfo)
        {
            var result = new List<ValueTreeNode>();

            if (fieldInfo.QuerySourceName == "OrderDetailsByRegion" && fieldInfo.Name == "CountryRegionName"
                && (HttpContext.Current.User.IsInRole("Manager") || HttpContext.Current.User.IsInRole("Employee")))
            {
                //Node [All] and [Blank] are required for UI to render.
                var rootNode = new ValueTreeNode { Text = "[All]", Value = "[All]" };
                rootNode.Nodes = new List<ValueTreeNode>();
                //rootNode.Nodes.Add(new ValueTreeNode { Text = "[Blank]", Value = "[Blank]" });


                if (HttpContext.Current.User.IsInRole("Manager"))
                {
                    rootNode.Nodes.Add(new ValueTreeNode { Text = "South America", Value = "South America" });
                    rootNode.Nodes.Add(new ValueTreeNode { Text = "North America", Value = "North America" });
                }

                if (HttpContext.Current.User.IsInRole("Employee"))
                {
                    rootNode.Nodes.Add(new ValueTreeNode { Text = "South America", Value = "South America" });
                }

                //else
                //{
                //    rootNode.Nodes.Add(new ValueTreeNode { Text = "Europe", Value = "Europe" });
                //    rootNode.Nodes.Add(new ValueTreeNode { Text = "South America", Value = "South America" });
                //    rootNode.Nodes.Add(new ValueTreeNode { Text = "North America", Value = "North America" });
                //}

                result.Add(rootNode);
            }

            return result;
        }

        public override ReportFilterSetting SetHiddenFilters(SetHiddenFilterParam param)
        {
            if (UserContext.Current?.CurrentUser != null && UserContext.Current.CurrentUser.IsSystemAdmin())
                return base.SetHiddenFilters(param);

            var filterFieldName = "ShipRegion";

            Func<ReportFilterSetting, int, QuerySource, QuerySourceField, Guid, Relationship, int> addHiddenFilters = (result, filterPosition, querySource, field, equalOperator, rel) =>
            {
                var firstFilter = new ReportFilterField
                {
                    Alias = $"ShipRegion{filterPosition}",
                    QuerySourceId = querySource.Id,
                    SourceDataObjectName = querySource.Name,
                    QuerySourceType = querySource.Type,
                    QuerySourceFieldId = field.Id,
                    SourceFieldName = field.Name,
                    DataType = field.DataType,
                    Position = ++filterPosition,
                    OperatorId = equalOperator,
                    Value = "WA",
                    RelationshipId = rel?.Id,
                    IsParameter = false,
                    ReportFieldAlias = null
                };

                var secondFilter = new ReportFilterField
                {
                    Alias = $"ShipRegion{filterPosition}",
                    QuerySourceId = querySource.Id,
                    SourceDataObjectName = querySource.Name,
                    QuerySourceType = querySource.Type,
                    QuerySourceFieldId = field.Id,
                    SourceFieldName = field.Name,
                    DataType = field.DataType,
                    Position = ++filterPosition,
                    OperatorId = equalOperator,
                    Value = "[Blank]",
                    RelationshipId = rel?.Id,
                    IsParameter = false,
                    ReportFieldAlias = null
                };
                result.FilterFields.Add(firstFilter);
                result.FilterFields.Add(secondFilter);

                var logic = $"({filterPosition - 1} OR {filterPosition})";
                if (string.IsNullOrEmpty(result.Logic))
                {
                    result.Logic = logic;
                }

                return filterPosition;
            };

            var filterSetting = new ReportFilterSetting()
            {
                FilterFields = new List<ReportFilterField>()
            };
            var position = 0;

            var ds = param.ReportDefinition.ReportDataSource;

            // Build the hidden filters for ship country fields
            foreach (var querySource in param.QuerySources // Scan thru the query sources that are involved in the report
                .Where(x => x.QuerySourceFields.Any(y => y.Name.Equals(filterFieldName, StringComparison.OrdinalIgnoreCase)))) // Take only query sources that have filter field name
            {
                // Pick the relationships that joins the query source as primary source
                // Setting the join ensure the proper table is assigned when using join alias in the UI
                var rels = param.ReportDefinition.ReportRelationship.
                    Where(x => x.JoinQuerySourceId == querySource.Id)
                    .ToList();

                // Count the relationships that the filter query source is foreign query source
                var foreignRelCounts = param.ReportDefinition.ReportRelationship
                    .Where(x => x.ForeignQuerySourceId == querySource.Id)
                    .Count();

                // Find actual filter field in query source
                var field = querySource.QuerySourceFields.FirstOrDefault(x => x.Name.Equals(filterFieldName, StringComparison.OrdinalIgnoreCase));

                // Pick the equal operator
                var equalOperator = Izenda.BI.Framework.Enums.FilterOperator.FilterOperator.EqualsManualEntry.GetUid();

                // In case there is no relationship that the query source is joined as primary
                if (rels.Count() == 0)
                {
                    // Just add hidden filter with null relationship
                    position = addHiddenFilters(filterSetting, position, querySource, field, equalOperator, null);
                }
                else
                {
                    // Add another hidden filter for query source that appears in both alias primary and foreign query source of relationships.
                    // This step is mandatory because when aliasing a primary query source, it becomes another instance of query source in the query. 
                    // So if we only add filter for alias, the original query source instance will not be impacted by the filter. That's why we need
                    // to add another filter for original instance when it appears in both side of alias and foreign.
                    // For example:
                    //          [Order] LEFT JOIN [Employee]
                    //      [Aliased Employee] LEFT JOIN [Department]
                    // If the system needs to add a hidden filter to [Employee], for example: [CompanyId] = 'ALKA'
                    // It needs to add
                    //          [Employee].[CompanyId] = 'ALKA' AND [Aliased Employee].[CompanyId] = 'ALKA'
                    // By this way, it ensures all [Employee] instances are filtered by ALKA company id.
                    if (foreignRelCounts > 0)
                    {
                        position = addHiddenFilters(filterSetting, position, querySource, field, equalOperator, null);
                    }
                    
                    foreach (var rel in rels)
                    {
                        // Loop thru all relationships that the query source is joined as primary and add the hidden field associated with each relationship
                        position = addHiddenFilters(filterSetting, position, querySource, field, equalOperator, rel);
                    }
                }
            }

            return filterSetting;
        }
    }
}