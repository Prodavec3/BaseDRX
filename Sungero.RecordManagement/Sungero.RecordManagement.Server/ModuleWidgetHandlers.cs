using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemCompletionGraphWidgetHandlers
  {

    public virtual void GetActionItemCompletionGraphActionItemCompletionGraphValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      var seriesList = Functions.Module.GetActionItemCompletionStatisticForChart(_parameters.Performer);
      
      if (!seriesList.Any())
        return;
      
      e.Chart.IsLegendVisible = false;
      foreach (var series in seriesList)
      {
        var seriesName = series.Month.ToString("MMM yyyy");
        if (seriesName != null)
        {
          var actionItemSeries = e.Chart.AddNewSeries(series.Month.ToString(), seriesName);
          actionItemSeries.DisplayValueFormat = series.Statistic.HasValue ? "{0}%" : "-";
          
          var count = series.Statistic.HasValue ? series.Statistic.Value : 0;
          
          actionItemSeries.AddValue(series.Month.ToString(), RecordManagement.Resources.ActionItemCompletion, count);
        }
      }
    }
  }

  partial class ActionItemsWidgetHandlers
  {

    public virtual IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> ActionItemsOverdueFiltering(IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> query)
    {
      return Functions.Module.GetActionItemsToWidgets(true, _parameters.Substitution);
    }

    public virtual IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> ActionItemsUnderControlFiltering(IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> query)
    {
      return Functions.Module.GetActionItemsToWidgets(false, _parameters.Substitution);
    }
  }
}