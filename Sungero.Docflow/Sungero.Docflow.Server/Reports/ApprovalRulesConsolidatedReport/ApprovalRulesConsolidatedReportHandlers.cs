using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.DatabookEntry;
using Sungero.Domain.Shared;

namespace Sungero.Docflow
{
  partial class ApprovalRulesConsolidatedReportServerHandlers
  {
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      
      #region Параметры
      
      var reportSessionId = System.Guid.NewGuid().ToString();
      ApprovalRulesConsolidatedReport.ReportSessionId = reportSessionId;
      var includeSubsidiary = ApprovalRulesConsolidatedReport.IncludeSubsidiary == true;
      var filterDepartment = ApprovalRulesConsolidatedReport.Department;
      var filterBusinessUnit = ApprovalRulesConsolidatedReport.BusinessUnit;
      var filterDepartmentsForBusinessUnits = ApprovalRulesConsolidatedReport.FilterDepartmentsForBusinessUnits == true;
      var filterDocumentFlow = ApprovalRulesConsolidatedReport.DocumentFlow;
      var filterDocumentKind = ApprovalRulesConsolidatedReport.DocumentKind;
      var filterCategory = ApprovalRulesConsolidatedReport.Category;
      
      #endregion
      
      #region Подготовка начальных данных
      
      var localizedValueCash = new Dictionary<Sungero.Core.Enumeration, string>();
      localizedValueCash[ApprovalRuleBase.Status.Active] = ApprovalRuleBases.Info.Properties.Status.GetLocalizedValue(ApprovalRuleBase.Status.Active);
      localizedValueCash[ApprovalRuleBase.Status.Draft] = ApprovalRuleBases.Info.Properties.Status.GetLocalizedValue(ApprovalRuleBase.Status.Draft);
      
      var documentFlows = new List<Enumeration>()
      { ApprovalRuleBase.DocumentFlow.Incoming,
        ApprovalRuleBase.DocumentFlow.Outgoing,
        ApprovalRuleBase.DocumentFlow.Inner,
        ApprovalRuleBase.DocumentFlow.Contracts };
      foreach (var flow in documentFlows)
        localizedValueCash[flow] = ApprovalRuleBases.Info.Properties.DocumentFlow.GetLocalizedValue(flow);
      
      var tableData = new List<Structures.ApprovalRulesConsolidatedReport.TableLine>();
      // Сформировать список активных видов документов, которые можно утверждать.
      // Чтобы не было перезапросов к СП, весь список хранить в памяти.
      var approvalAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      var rules = ApprovalRuleBases.GetAll().Where(r => r.Status == ApprovalRuleBase.Status.Active ||
                                                   r.Status == ApprovalRuleBase.Status.Draft).ToList();
      #endregion
      
      #region Фильтрация
      
      // НОР.
      List<Company.IBusinessUnit> businessUnits;
      if (filterBusinessUnit != null)
      {
        businessUnits = new List<Company.IBusinessUnit>() { filterBusinessUnit };
        if (includeSubsidiary)
        {
          businessUnits = this.GetSubBusinessUnits(businessUnits, Company.BusinessUnits.GetAll().Where(u => u.Status == Status.Active).ToList());
        }
      }
      else
      {
        businessUnits = Company.BusinessUnits.GetAll().Where(u => u.Status == Status.Active).ToList();
        
        // Для подразделений без НОР.
        if (filterDepartmentsForBusinessUnits)
          businessUnits.Add(null);
      }
      
      // Подразделения.
      List<Company.IDepartment> departments;
      if (filterDepartment != null)
      {
        departments = new List<Company.IDepartment>() { filterDepartment };
        if (includeSubsidiary)
        {
          departments = this.GetSubDepartments(departments, Company.Departments.GetAll().Where(u => u.Status == Status.Active).ToList());
        }
      }
      else
      {
        departments = Company.Departments.GetAll().Where(u => u.Status == Status.Active).ToList();
      }
      
      // Виды документов.
      List<IDocumentKind> documentKinds;
      if (filterDocumentKind != null)
      {
        documentKinds = new List<IDocumentKind>() { filterDocumentKind };
      }
      else
      {
        documentKinds = DocumentKinds.GetAll()
          .Where(k => k.Status == DocumentKind.Status.Active && k.AvailableActions.Any(a => a.Action == approvalAction))
          .ToList();
      }
      
      // Категории договоров.
      List<IDocumentGroupBase> categories;
      if (filterCategory != null)
      {
        categories = new List<IDocumentGroupBase>() { filterCategory };
      }
      else
      {
        categories = DocumentGroupBases.GetAll().Where(d => d.Status == Status.Active).OrderBy(d => d.Name).ToList();
      }
      
      // Документопоток.
      if (!string.IsNullOrEmpty(filterDocumentFlow))
        documentFlows = documentFlows.Where(x => localizedValueCash[x] == filterDocumentFlow).ToList();
      
      #endregion
      
      var tableLines = new List<Structures.ApprovalRulesConsolidatedReport.TableLine>();
      
      foreach (var unit in businessUnits)
      {
        var subDepartments = departments.Where(d => (!filterDepartmentsForBusinessUnits && unit != null) || Equals(d.BusinessUnit, unit)).OrderBy(d => d.Name).ToList();
        
        if (!subDepartments.Any() &&
            unit != null &&
            filterDepartment == null)
          subDepartments.Add(null);
        
        foreach (var department in subDepartments)
        {
          foreach (var flow in documentFlows)
          {
            foreach (var kind in documentKinds.Where(k => Equals(k.DocumentFlow, flow)))
            {
              var subRules = rules
                .Where(r => r.DocumentFlow == flow &&
                       (!r.DocumentKinds.Any() || r.DocumentKinds.Any(k => Equals(k.DocumentKind, kind))) &&
                       (!r.BusinessUnits.Any() || r.BusinessUnits.Any(u => Equals(u.BusinessUnit, unit))) &&
                       (!r.Departments.Any() || r.Departments.Any(d => Equals(d.Department, department))));
              var documentParentType = this.GetDocumentBaseTypeDisplayName(kind);
              
              // Отдельно обрабатываем виды документов, для которых настроены категории.
              var documentKindCategories = categories.Where(c => c.DocumentKinds.Any(d => Equals(d.DocumentKind, kind)));
              if (documentKindCategories.Any())
              {
                foreach (var category in documentKindCategories)
                {
                  var categorySubRules = subRules.Where(sr => !sr.DocumentGroups.Any() || sr.DocumentGroups.Any(c => Equals(c.DocumentGroup, category)));
                  tableLines = this.CreateTableDataLines(categorySubRules, reportSessionId, unit, department, category, flow, kind.Name, documentParentType, localizedValueCash);
                  tableData.AddRange(tableLines);
                }
                continue;
              }
              
              // Если выбрана фильтрация по категории, то в отчет попадают только записи с заполненной категорией.
              if (filterCategory == null)
              {
                // Если не найдено правила для доп. соглашений, тогда указываем, что используется правило договора.
                var isSupAgreement = kind.DocumentType.DocumentTypeGuid == "265f2c57-6a8a-4a15-833b-ca00e8047fa5";
                if (isSupAgreement)
                {
                  tableLines = this.CreateTableDataLines(subRules, reportSessionId, unit, department, null, flow, kind.Name, documentParentType, localizedValueCash);
                  foreach (var tableLine in tableLines)
                  {
                    if (string.IsNullOrEmpty(tableLine.ApprovalRule))
                      tableLine.ApprovalRule = Reports.Resources.ApprovalRulesConsolidatedReport.ContractualDocumentRule;
                  }
                  tableData.AddRange(tableLines);
                  continue;
                }
                
                tableLines = this.CreateTableDataLines(subRules, reportSessionId, unit, department, null, flow, kind.Name, documentParentType, localizedValueCash);
                tableData.AddRange(tableLines);
              }
            }
          }
        }
      }
      
      Functions.Module.WriteStructuresToTable(Constants.ApprovalRulesConsolidatedReport.SourceTableName, tableData);
    }
    
    /// <summary>
    /// Получить подчиненные НОР.
    /// </summary>
    /// <param name="headBusinessUnits">Список головных организаций.</param>
    /// <param name="businessUnits">Список НОР, в котором осуществляется поиск подчиненных НОР.</param>
    /// <returns>Список, состоящий из головных НОР из входного параметра и их подчиненных.</returns>
    public List<Company.IBusinessUnit> GetSubBusinessUnits(List<Company.IBusinessUnit> headBusinessUnits, List<Company.IBusinessUnit> businessUnits)
    {
      var subBusinessUnits = businessUnits.Where(u => headBusinessUnits.Any(b => Equals(b, u.HeadCompany))).ToList();
      if (subBusinessUnits.Any())
      {
        var result = this.GetSubBusinessUnits(subBusinessUnits, businessUnits);
        headBusinessUnits.AddRange(result);
      }
      return headBusinessUnits;
    }
    
    /// <summary>
    /// Получить подчиненные подразделения.
    /// </summary>
    /// <param name="headDepartments">Список головных подразделений.</param>
    /// <param name="departments">Список подразделений, в котором осуществляется поиск подчиненных подразделений.</param>
    /// <returns>Список, состоящий из головных подразделений из входного параметра и их подчиненных.</returns>
    public List<Company.IDepartment> GetSubDepartments(List<Company.IDepartment> headDepartments, List<Company.IDepartment> departments)
    {
      var subDepartments = departments.Where(u => headDepartments.Any(b => Equals(b, u.HeadOffice))).ToList();
      if (subDepartments.Any())
      {
        var result = this.GetSubDepartments(subDepartments, departments);
        headDepartments.AddRange(result);
      }
      return headDepartments;
    }
    
    /// <summary>
    /// Получить отображаемое имя базового типа сущности по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Отображаемое имя.</returns>
    /// <remarks>В качестве базового типа берется тип, который является прямым наследником от официального документа.</remarks>
    public string GetDocumentBaseTypeDisplayName(IDocumentKind documentKind)
    {
      var documentTypeGuid = Guid.Parse(documentKind.DocumentType.DocumentTypeGuid);
      var documentType = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(documentTypeGuid);
      var documentMetadata = Sungero.Domain.Shared.TypeExtension.GetEntityMetadata(documentType);
      
      while (documentMetadata.BaseEntityMetadata.NameGuid != Docflow.Server.OfficialDocument.ClassTypeGuid)
        documentMetadata = documentMetadata.BaseEntityMetadata;
      
      return documentMetadata.GetDisplayName();
    }
    
    public List<Structures.ApprovalRulesConsolidatedReport.TableLine> CreateTableDataLines(IEnumerable<IApprovalRuleBase> rules,
                                                                                           string reportSessionId,
                                                                                           Sungero.Company.IBusinessUnit businessUnit,
                                                                                           Sungero.Company.IDepartment department,
                                                                                           Sungero.Docflow.IDocumentGroupBase category,
                                                                                           Sungero.Core.Enumeration flow,
                                                                                           string documentKind,
                                                                                           string documentParentType,
                                                                                           Dictionary<Sungero.Core.Enumeration, string> localizedValueCash)
    {
      var result = new List<Structures.ApprovalRulesConsolidatedReport.TableLine>();
      if (!rules.Any())
      {
        var line = this.CreateTableLine(reportSessionId, businessUnit, department, category, null, flow, documentKind, documentParentType, localizedValueCash);
        result.Add(line);
      }
      
      foreach (var rule in rules)
      {
        var line = this.CreateTableLine(reportSessionId, businessUnit, department, category, rule, flow, documentKind, documentParentType, localizedValueCash);
        result.Add(line);
      }
      return result;
    }
    
    public Structures.ApprovalRulesConsolidatedReport.TableLine CreateTableLine(string reportSessionId,
                                                                                Sungero.Company.IBusinessUnit businessUnit,
                                                                                Sungero.Company.IDepartment department,
                                                                                Sungero.Docflow.IDocumentGroupBase category,
                                                                                Sungero.Docflow.IApprovalRuleBase rule,
                                                                                Sungero.Core.Enumeration flow,
                                                                                string documentKind,
                                                                                string documentParentType,
                                                                                Dictionary<Sungero.Core.Enumeration, string> localizedValueCash)
    {
      var line = Structures.ApprovalRulesConsolidatedReport.TableLine.Create();
      line.BusinessUnit = businessUnit != null ? businessUnit.Name : string.Empty;
      line.Department = department != null ? department.Name : string.Empty;
      line.ReportSessionId = reportSessionId;
      line.Relation = department != null && businessUnit != null && Equals(department.BusinessUnit, businessUnit) ? "+" : string.Empty;
      line.Category = category != null ? category.Name : string.Empty;
      if (rule != null)
      {
        line.ApprovalRule = rule.Name;
        line.ApprovalRuleId = rule.Id;
        line.ApprovalRulePriority = rule.Priority;
        line.ApprovalRuleUrl = Hyperlinks.Get(rule);
        line.Status = localizedValueCash[rule.Status.Value];
      }
      line.DocumentFlow = localizedValueCash[flow];
      line.DocumentKind = documentKind;
      line.DocumentParentType = documentParentType;
      return line;
    }
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApprovalRulesConsolidatedReport.SourceTableName, ApprovalRulesConsolidatedReport.ReportSessionId);
    }

  }
}