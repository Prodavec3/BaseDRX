using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class ApprovalRulesConsolidatedReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      // Диалог.
      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.ApprovalRulesConsolidatedReport);
      dialog.HelpCode = Constants.ApprovalRulesConsolidatedReport.HelpCode;
      var businessUnit = dialog.AddSelect(Docflow.Resources.BusinessUnit, false, Sungero.Company.BusinessUnits.Null)
        .From(Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnits().ToArray());
      var department = dialog.AddSelect(Docflow.Resources.Department, false, Sungero.Company.Departments.Null)
        .From(this.GetFilteredDepartments(null, false).ToArray());
      var includeSubsidiary = dialog.AddBoolean(Reports.Resources.ApprovalRulesConsolidatedReport.IncludeSubsidiary, true);
      var filterDepartmentsForBusinessUnits = dialog.AddBoolean(Reports.Resources.ApprovalRulesConsolidatedReport.FilterDepartmentsForBusinessUnitsDialogCheckBox, true);
      var availableDocumentFlows = new List<Enumeration?>() { Docflow.DocumentKind.DocumentFlow.Incoming,  Docflow.DocumentKind.DocumentFlow.Outgoing,
        Docflow.DocumentKind.DocumentFlow.Inner, Docflow.DocumentKind.DocumentFlow.Contracts };
      var documentFlows = availableDocumentFlows.Select(a => DocumentKinds.Info.Properties.DocumentFlow.GetLocalizedValue(a)).ToList();
      var documentFlow = dialog.AddSelect(Docflow.Resources.Direction, false).From(documentFlows.ToArray());
      var documentKind = dialog.AddSelect(Docflow.Resources.DocumentKind, false, Sungero.Docflow.DocumentKinds.Null);
      var category = dialog.AddSelect(Docflow.Resources.Category, false, Sungero.Docflow.DocumentGroupBases.Null);
      
      // События.
      businessUnit.SetOnValueChanged((arg) =>
                                     {
                                       if (Equals(arg.NewValue, arg.OldValue))
                                         return;
                                       
                                       if (department.Value != null && !Equals(arg.NewValue, department.Value.BusinessUnit))
                                         department.Value = Sungero.Company.Departments.Null;
                                       
                                       department.From(this.GetFilteredDepartments(arg.NewValue, filterDepartmentsForBusinessUnits.Value));
                                     });
      
      department.SetOnValueChanged((arg) =>
                                   {
                                     if (!Equals(arg.NewValue, arg.OldValue) && arg.NewValue != null)
                                       businessUnit.Value = arg.NewValue.BusinessUnit;
                                   });
      
      documentFlow.SetOnValueChanged((arg) =>
                                     {
                                       if (!Equals(arg.NewValue, arg.OldValue))
                                       {
                                         var docFlow = !string.IsNullOrEmpty(arg.NewValue) ?
                                           availableDocumentFlows[documentFlows.IndexOf(arg.NewValue)] : null;
                                         documentKind.From(this.GetFilteredDocumentKinds(docFlow));
                                         category.From(this.GetFilteredCategories(documentKind.Value, docFlow));
                                         if (documentKind.Value != null && documentKind.Value.DocumentFlow != docFlow)
                                         {
                                           documentKind.Value = Sungero.Docflow.DocumentKinds.Null;
                                           category.Value = Sungero.Docflow.DocumentGroupBases.Null;
                                         }
                                       }
                                     });
      
      documentKind.SetOnValueChanged((arg) =>
                                     {
                                       if (!Equals(arg.NewValue, arg.OldValue))
                                       {
                                         var docFlow = !string.IsNullOrEmpty(documentFlow.Value) ?
                                           availableDocumentFlows[documentFlows.IndexOf(documentFlow.Value)] : null;
                                         category.From(this.GetFilteredCategories(arg.NewValue, docFlow));
                                         if (arg.NewValue != null)
                                           documentFlow.Value = DocumentKinds.Info.Properties.DocumentFlow.GetLocalizedValue(arg.NewValue.DocumentFlow);
                                         
                                         if (category.Value != null && category.Value.DocumentKinds.Any() &&
                                             !category.Value.DocumentKinds.Any(x => Equals(x.DocumentKind, arg.NewValue)))
                                           category.Value = Sungero.Docflow.DocumentGroupBases.Null;
                                       }
                                     });
      
      category.SetOnValueChanged((arg) =>
                                 {
                                   if (!Equals(arg.NewValue, arg.OldValue))
                                   {
                                     if (arg.NewValue != null && arg.NewValue.DocumentKinds.Count == 1)
                                       documentKind.Value = arg.NewValue.DocumentKinds.Single().DocumentKind;
                                   }
                                 });
      
      filterDepartmentsForBusinessUnits
        .SetOnValueChanged((arg) =>
                           {
                             department.From(this.GetFilteredDepartments(businessUnit.Value, arg.NewValue));
                           });
      
      // Показ диалога.
      if (dialog.Show() == DialogButtons.Ok)
      {
        if (businessUnit.Value != null)
          ApprovalRulesConsolidatedReport.BusinessUnit = businessUnit.Value;
        if (department.Value != null)
          ApprovalRulesConsolidatedReport.Department = department.Value;
        if (includeSubsidiary != null)
          ApprovalRulesConsolidatedReport.IncludeSubsidiary = includeSubsidiary.Value;
        if (filterDepartmentsForBusinessUnits != null)
          ApprovalRulesConsolidatedReport.FilterDepartmentsForBusinessUnits = filterDepartmentsForBusinessUnits.Value;
        if (!string.IsNullOrEmpty(documentFlow.Value))
          ApprovalRulesConsolidatedReport.DocumentFlow = documentFlow.Value;
        if (documentKind != null)
          ApprovalRulesConsolidatedReport.DocumentKind = documentKind.Value;
        if (category != null)
          ApprovalRulesConsolidatedReport.Category = category.Value;
      }
      else
        e.Cancel = true;
    }
    
    private List<IDepartment> GetFilteredDepartments(IBusinessUnit businessUnit, bool? filterDepartmentsForBusinessUnits)
    {
      var departments = Sungero.Company.PublicFunctions.Department.Remote.GetDepartments()
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Подразделения фильтруются по НОР.
      if (filterDepartmentsForBusinessUnits == true && businessUnit != null)
        return departments
          .Where(d => Equals(d.BusinessUnit, businessUnit))
          .ToList();

      // Подразделения не фильтруются по НОР.
      return departments.ToList();
    }
    
    private IDocumentKind[] GetFilteredDocumentKinds(Enumeration? documentFlow)
    {
      var documentKinds = Functions.DocumentKind.Remote.GetDocumentKinds();
      if (documentFlow != null)
        documentKinds = documentKinds.Where(k => k.DocumentFlow == documentFlow);
      
      return documentKinds.ToArray();
    }
    
    private IDocumentGroupBase[] GetFilteredCategories(IDocumentKind documentKind, Enumeration? documentFlow)
    {
      var categories = Docflow.PublicFunctions.DocumentGroupBase.Remote.GetDocumentGroups();
      if (documentFlow != null)
        categories = categories.Where(c => c.DocumentKinds.All(k => k.DocumentKind.DocumentFlow == documentFlow));
      
      if (documentKind != null)
        categories = categories.Where(c => c.DocumentKinds.Any(k => Equals(k.DocumentKind, documentKind)) || !c.DocumentKinds.Any());
      
      return categories.ToArray();
    }
  }
}