using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.DatabookEntry;

namespace Sungero.Docflow
{
  partial class ApprovalRuleCardReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApprovalRuleCardReport.CriteriaTableName, ApprovalRuleCardReport.ReportSessionId);
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApprovalRuleCardReport.ConditionTableName, ApprovalRuleCardReport.ReportSessionId);
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApprovalRuleCardReport.SignatureSettingsTableName, ApprovalRuleCardReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var entity = ApprovalRuleCardReport.ApprovalRule;
      ApprovalRuleCardReport.ReportSessionId = System.Guid.NewGuid().ToString();
      ApprovalRuleCardReport.CurrentDate = Calendar.Now;
      var tableData = new List<Structures.ApprovalRuleCardReport.CriteriaTableLine>();
      
      // Основные свойства.
      ApprovalRuleCardReport.RuleFlowLocalized = entity.Info.Properties.DocumentFlow.GetLocalizedValue(entity.DocumentFlow.Value);
      ApprovalRuleCardReport.RuleStatusLocalized = entity.Info.Properties.Status.GetLocalizedValue(entity.Status.Value);
      
      // Доработка.
      if (entity.ReworkPerformerType == ApprovalRule.ReworkPerformerType.EmployeeRole)
        ApprovalRuleCardReport.ReworkPerformer = Functions.ApprovalStage.GetRecipientDescription(entity.ReworkPerformer);
      if (entity.ReworkPerformerType == ApprovalRule.ReworkPerformerType.Author)
        ApprovalRuleCardReport.ReworkPerformer = Sungero.Docflow.ApprovalStages.Resources.ReportAuthor;
      if (entity.ReworkPerformerType == ApprovalRule.ReworkPerformerType.ApprovalRole)
        ApprovalRuleCardReport.ReworkPerformer = string.Format("\"{0}\"", entity.ReworkApprovalRole.Name);
      ApprovalRuleCardReport.ReworkDeadline = string.Format("{0} {1}",
                                                            entity.ReworkDeadline,
                                                            Functions.Module.GetNumberDeclination(entity.ReworkDeadline.Value, Resources.StateViewDay, Resources.StateViewDayGenetive, Resources.StateViewDayPlural));
      
      // Критерии.
      // НОР.
      var propertyLocalizedName = ApprovalRuleBases.Info.Properties.BusinessUnits.LocalizedName;
      var businessUnits = ApprovalRuleCardReport.ApprovalRule.BusinessUnits;
      foreach (var businessUnit in businessUnits)
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, businessUnit.BusinessUnit));
      if (!businessUnits.Any())
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, Reports.Resources.ApprovalRuleCardReport.All));
      
      // Подразделения.
      propertyLocalizedName = ApprovalRuleBases.Info.Properties.Departments.LocalizedName;
      var departments = ApprovalRuleCardReport.ApprovalRule.Departments;
      foreach (var department in departments)
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, department.Department));
      if (!departments.Any())
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, Reports.Resources.ApprovalRuleCardReport.All));
      
      // Виды документов.
      propertyLocalizedName = ApprovalRuleBases.Info.Properties.DocumentKinds.LocalizedName;
      var documentKinds = ApprovalRuleCardReport.ApprovalRule.DocumentKinds;
      foreach (var documentKind in documentKinds)
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, documentKind.DocumentKind));
      if (!documentKinds.Any())
        tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName,
                                                   Reports.Resources.ApprovalRuleCardReport.AllKindsFormat(ApprovalRuleCardReport.RuleFlowLocalized)));
      
      // Категории договоров, если правило для договорных.
      if (Sungero.Contracts.ContractsApprovalRules.Is(entity))
      {
        propertyLocalizedName = Sungero.Contracts.ContractsApprovalRules.Info.Properties.DocumentGroups.LocalizedName;
        var documentGroups = ApprovalRuleCardReport.ApprovalRule.DocumentGroups;
        foreach (var documentGroup in documentGroups)
          tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, documentGroup.DocumentGroup));
        if (!documentGroups.Any())
          tableData.Add(this.CreateCriteriaTableLine(propertyLocalizedName, Reports.Resources.ApprovalRuleCardReport.All));
      }
      
      Functions.Module.WriteStructuresToTable(Constants.ApprovalRuleCardReport.CriteriaTableName, tableData);
      
      var conditionTableData = this.GetConditionTableData(entity, ApprovalRuleCardReport.ReportSessionId);
      Functions.Module.WriteStructuresToTable(Constants.ApprovalRuleCardReport.ConditionTableName, conditionTableData);
      
      // Добавить права подписи.
      if (entity.Stages.Any(x => x.StageType == Docflow.ApprovalRuleStages.StageType.Sign ||
                            x.StageType == Docflow.ApprovalRuleStages.StageType.Review))
      {
        ApprovalRuleCardReport.SignSettHeader = Reports.Resources.ApprovalRuleCardReport.SignatureSettingsHeader;
        var signatureSettingsData = this.GetSignatureSettingsTableData(entity, ApprovalRuleCardReport.ReportSessionId);
        if (!signatureSettingsData.Any())
          ApprovalRuleCardReport.SignSettEmpty = Reports.Resources.ApprovalRuleCardReport.SignatureSettingsEmpty;
        Functions.Module.WriteStructuresToTable(Constants.ApprovalRuleCardReport.SignatureSettingsTableName, signatureSettingsData);
      }
    }
    
    #region Создание структур для заполнения таблиц отчета
    
    public Structures.ApprovalRuleCardReport.CriteriaTableLine CreateCriteriaTableLine(string criterion, string value)
    {
      var tableLine = new Structures.ApprovalRuleCardReport.CriteriaTableLine();
      tableLine.ReportSessionId = ApprovalRuleCardReport.ReportSessionId;
      tableLine.Criterion = criterion;
      tableLine.Value = value;
      return tableLine;
    }
    
    public Structures.ApprovalRuleCardReport.CriteriaTableLine CreateCriteriaTableLine(string criterion, Sungero.CoreEntities.IDatabookEntry value)
    {
      var tableLine = new Structures.ApprovalRuleCardReport.CriteriaTableLine();
      tableLine.ReportSessionId = ApprovalRuleCardReport.ReportSessionId;
      tableLine.Criterion = criterion;
      if (value != null)
      {
        tableLine.Value = value.DisplayValue;
        tableLine.ValueId = value.Id;
        tableLine.ValueHyperlink = Hyperlinks.Get(value);
      }
      return tableLine;
    }
    
    private Structures.ApprovalRuleCardReport.SignatureSettingsTableLine CreateSignatureSettingsTableLine(string displayValue, int id, string hyperlink, int orderNumber,
                                                                                                          string unitsAndDeps, string kindsAndCategories, int priority,
                                                                                                          string limits, string validTill, string note)
    {
      var tableLine = Structures.ApprovalRuleCardReport.SignatureSettingsTableLine.Create();
      tableLine.ReportSessionId = ApprovalRuleCardReport.ReportSessionId;
      tableLine.Name = displayValue;
      tableLine.Id = id;
      tableLine.Hyperlink = hyperlink;
      tableLine.OrderNumber = orderNumber;
      tableLine.UnitsAndDeps = unitsAndDeps;
      tableLine.KindsAndCategories = kindsAndCategories;
      tableLine.Priority = priority;
      tableLine.Limits = limits;
      tableLine.ValidTill = validTill;
      tableLine.Note = note;
      
      return tableLine;
    }
    
    #endregion
    
    #region Получение данных для таблиц отчета
    
    #region Данные для таблицы прав подписи
    
    /// <summary>
    /// Получить данные для отображения прав подписи.
    /// </summary>
    /// <param name="rule">Правило согласования.</param>
    /// <param name="reportSessionId">ID отчета.</param>
    /// <returns>Список структур с данными о правах подписи.</returns>
    private List<Structures.ApprovalRuleCardReport.SignatureSettingsTableLine> GetSignatureSettingsTableData(IApprovalRuleBase rule, string reportSessionId)
    {
      var tableData = new List<Structures.ApprovalRuleCardReport.SignatureSettingsTableLine>();
      var signatureSettings = this.GetSignatureSettings(rule)
        .OrderByDescending(x => x.Priority).ToList();
      
      foreach (var signSetting in signatureSettings)
      {
        var displayValue = this.GetDisplayValuePresentation(signSetting);
        var id = signSetting.Id;
        var hyperlink = Hyperlinks.Get(signSetting);
        var orderNumber = signatureSettings.IndexOf(signSetting);
        var unitsAndDeps = this.GetBusinessUnitsAndDepartmentsPresentation(signSetting);
        var kindsAndCategories = this.GetDocumentKindsAndCategoriesPresentation(signSetting);
        var priority = signSetting.Priority ?? 0;
        
        // Лимит.
        var limits = string.Empty;
        if ((signSetting.DocumentFlow == SignatureSetting.DocumentFlow.Contracts ||
             signSetting.DocumentFlow == SignatureSetting.DocumentFlow.All) &&
            signSetting.Limit == SignatureSetting.Limit.Amount &&
            signSetting.Amount.HasValue && signSetting.Currency != null)
        {
          limits = string.Format("{0} {1}",
                                 signSetting.Amount.Value.ToString("N2"),
                                 signSetting.Currency.AlphaCode);
        }
        
        // Срок.
        var validTill = string.Empty;
        if (signSetting.ValidTill.HasValue)
          validTill = signSetting.ValidTill.Value.ToShortDateString();
        
        // Примечание.
        var note = signSetting.Note;
        
        tableData.Add(this.CreateSignatureSettingsTableLine(displayValue, id, hyperlink, orderNumber, unitsAndDeps, kindsAndCategories, priority, limits, validTill, note));
      }
      
      return tableData;
    }
    
    /// <summary>
    /// Получить представление права подписи для таблицы.
    /// </summary>
    /// <param name="signSetting">Право подписи.</param>
    /// <returns>Строковое представление права подписи для таблицы.</returns>
    private string GetDisplayValuePresentation(ISignatureSetting signSetting)
    {
      var displayValue = new StringBuilder();
      displayValue.AppendFormat("<b>{0}</b>", signSetting.DisplayValue);
      
      // Добавить должность (если подписывающий - сотрудник).
      var employee = Sungero.Company.Employees.As(signSetting.Recipient);
      if (employee != null && employee.JobTitle != null)
        displayValue.AppendFormat(" ({0})", employee.JobTitle.Name.Trim());
      displayValue.AppendLine();
      
      // Основание.
      displayValue.AppendLine();
      displayValue.AppendLine(string.Format("{0}: {1}", SignatureSettings.Info.Properties.Reason.LocalizedName,
                                            SignatureSettings.Info.Properties.Reason.GetLocalizedValue(signSetting.Reason)));
      
      // Приоритет.
      displayValue.AppendLine();
      displayValue.AppendLine(string.Format("{0}: {1}", SignatureSettings.Info.Properties.Priority.LocalizedName,
                                            signSetting.Priority ?? 0));
      
      return displayValue.ToString();
    }
    
    /// <summary>
    /// Получить представление НОР и подразделений права подписи.
    /// </summary>
    /// <param name="signSetting">Право подписи.</param>
    /// <returns>Строковой представление НОР и подразделений права подписи.</returns>
    private string GetBusinessUnitsAndDepartmentsPresentation(ISignatureSetting signSetting)
    {
      var presentation = new StringBuilder();
      var defaultValue = Reports.Resources.ApprovalRuleCardReport.All;
      
      // Наши организации.
      var name = SignatureSettings.Info.Properties.BusinessUnits.LocalizedName;
      var items = this.FormatStringsForPresentation(signSetting.BusinessUnits.Select(x => x.BusinessUnit.Name));
      this.UpdateCellPresentation(ref presentation, name, items, defaultValue);
      
      // Подразделения.
      presentation.AppendLine();
      name = SignatureSettings.Info.Properties.Departments.LocalizedName;
      items = this.FormatStringsForPresentation(signSetting.Departments.Select(x => x.Department.Name));
      this.UpdateCellPresentation(ref presentation, name, items, defaultValue);
      
      return presentation.ToString();
    }
    
    /// <summary>
    /// Получить представление видов и категорий документов.
    /// </summary>
    /// <param name="signSetting">Право подписи.</param>
    /// <returns>Строковое представление видов и категорий документов.</returns>
    private string GetDocumentKindsAndCategoriesPresentation(ISignatureSetting signSetting)
    {
      var presentation = new StringBuilder();
      
      // Виды документов.
      var name = SignatureSettings.Info.Properties.DocumentKinds.LocalizedName;
      var documentFlowLocalizedValue = SignatureSettings.Info.Properties.DocumentFlow.GetLocalizedValue(signSetting.DocumentFlow.Value);
      var defaultValue = signSetting.DocumentFlow == SignatureSetting.DocumentFlow.All
        ? Reports.Resources.ApprovalRuleCardReport.All
        : Reports.Resources.ApprovalRuleCardReport.AllKindsFormat(documentFlowLocalizedValue);
      var items = this.FormatStringsForPresentation(signSetting.DocumentKinds.Select(x => x.DocumentKind.Name));
      this.UpdateCellPresentation(ref presentation, name, items, defaultValue);
      
      // Категории договоров.
      if (Functions.SignatureSetting.GetPossibleCashedCategories(signSetting).Any() ||
          signSetting.Categories.Any())
      {
        presentation.AppendLine(string.Empty);
        name = SignatureSettings.Info.Properties.Categories.LocalizedName;
        defaultValue = Reports.Resources.ApprovalRuleCardReport.All;
        items = this.FormatStringsForPresentation(signSetting.Categories.Select(x => x.Category.Name));
        this.UpdateCellPresentation(ref presentation, name, items, defaultValue);
      }
      
      return presentation.ToString();
    }
    
    /// <summary>
    /// Отформатировать наименования так, чтобы все заканчивались на ';', а последнее на '.'.
    /// </summary>
    /// <param name="source">Исходный набор строк.</param>
    /// <returns>Отформатированный набор строк.</returns>
    private IEnumerable<string> FormatStringsForPresentation(IEnumerable<string> source)
    {
      return string.Format("{0}.", string.Join(";^", source))
        .Split(new string[] { "^" }, System.StringSplitOptions.None)
        .Except(new string[] { "." });
    }
    
    /// <summary>
    /// Обновить представление контента в ячейке.
    /// </summary>
    /// <param name="content">Контент (StringBuilder).</param>
    /// <param name="propName">Наименование свойства.</param>
    /// <param name="items">Значения.</param>
    /// <param name="defaultValue">Значение, если коллекция пуста.</param>
    private void UpdateCellPresentation(ref StringBuilder content, string propName, IEnumerable<string> items, string defaultValue)
    {
      content.AppendFormat("{0}:", propName);
      if (items.Count() == 0)
        content.AppendFormat(" {0}", defaultValue);
      content.AppendLine();
      foreach (var item in items)
        content.AppendLine(item);
    }
    
    #endregion
    
    public List<Structures.ApprovalRuleCardReport.ConditionTableLine> GetConditionTableData(IApprovalRuleBase rule, string reportSessionId)
    {
      var tableData = new List<Structures.ApprovalRuleCardReport.ConditionTableLine>();
      var firstStage = rule.Transitions.Select(x => x.SourceStage).FirstOrDefault(s => !rule.Transitions.Any(t => t.TargetStage.Equals(s)));
      if (rule.Stages.Count == 1 && !rule.Conditions.Any() && !firstStage.HasValue)
        firstStage = rule.Stages.Single().Number;
      
      tableData = this.GetRouteDescription(rule, firstStage);
      var id = 0;
      tableData[0].ReportSessionId = reportSessionId;
      tableData[0].Id = id;
      for (var i = 1; i < tableData.Count; i++)
      {
        tableData[i].ReportSessionId = reportSessionId;
        if (!Equals(tableData[i - 1].Header, tableData[i].Header))
          id++;
        tableData[i].Id = id;
      }
      return tableData;
    }
    
    private List<Structures.ApprovalRuleCardReport.ConditionTableLine> GetRouteDescription(IApprovalRuleBase rule, int? lastNumber)
    {
      var linedRoute = new List<Structures.ApprovalRuleCardReport.ConditionTableLine>();
      while (true)
      {
        // Последний этап блок (не условие).
        if (rule.Stages.Any(s => s.Number == lastNumber))
        {
          var stage = rule.Stages.First(s => s.Number == lastNumber).StageBase;
          Functions.ApprovalStageBase.AddStageToRoute(stage, linedRoute, string.Empty, 0);
          var lastTransition = rule.Transitions.FirstOrDefault(s => s.SourceStage == lastNumber);
          if (lastTransition != null)
          {
            lastNumber = lastTransition.TargetStage.Value;
          }
          else
          {
            break;
          }
        }
        else if (rule.Conditions.Any(s => s.Number == lastNumber))
        {
          var resources = Reports.Resources.ApprovalRuleCardReport;
          var condition = rule.Conditions.First(s => s.Number == lastNumber).Condition;
          this.AddConditionToRoute(rule, linedRoute, condition, true,  resources.If, 0);
          
          // Переход по условию по ветке true.
          var nextNumber = lastNumber;
          var trueResultTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber && t.ConditionValue == true);
          var beforeStagesLinesCount = linedRoute.Count;
          if (trueResultTransition != null)
          {
            nextNumber = trueResultTransition.TargetStage.Value;
            var steps = this.GetLinedSteps(rule, linedRoute, ref nextNumber);
            this.AddStepsToRoute(rule, linedRoute, steps);
          }
          if (linedRoute.Count == beforeStagesLinesCount)
            this.AddEmptyLine(linedRoute, 1);
          
          // Переход по условию по ветке false.
          this.AddConditionToRoute(rule, linedRoute, condition, false, resources.ElseIf, 0);
          var falseResultTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber && t.ConditionValue == false);
          if (falseResultTransition != null)
          {
            nextNumber = falseResultTransition.TargetStage.Value;
            var steps = this.GetLinedSteps(rule, linedRoute, ref nextNumber);
            beforeStagesLinesCount = linedRoute.Count;
            this.AddStepsToRoute(rule, linedRoute, steps);
            if (linedRoute.Count == beforeStagesLinesCount)
              this.AddEmptyLine(linedRoute, 1);
          }
          else
          {
            this.AddEmptyLine(linedRoute, 1);
          }
          
          if (lastNumber == nextNumber)
            break;
          
          lastNumber = nextNumber;
          this.AddConditionToRoute(rule, linedRoute, condition, false, resources.EndIf, 0);
        }
        else
        {
          break;
        }
      }
      return linedRoute;
    }
    
    private List<List<Structures.ApprovalRuleCardReport.Transition>> GetLinedSteps(IApprovalRuleBase rule,
                                                                                   List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute,
                                                                                   ref int? lastNumber)
    {
      var steps = new List<List<Structures.ApprovalRuleCardReport.Transition>>();
      var nextNumber = lastNumber;
      var variants = Functions.ApprovalRuleBase.GetAllStagesVariants(rule);
      foreach (var variant in variants.AllSteps.OrderBy(v => string.Join(string.Empty, v)))
      {
        var variantSteps = new List<Structures.ApprovalRuleCardReport.Transition>();
        foreach (var stage in variant)
        {
          if (stage == lastNumber || variantSteps.Any())
          {
            // Завершить ветку, когда дошли до внешнего конца ветвления.
            if (variants.AllSteps.All(s => s.Contains(stage)))
            {
              steps.Add(variantSteps);
              variantSteps = new List<Structures.ApprovalRuleCardReport.Transition>();
              nextNumber = stage;
              break;
            }
            
            if (rule.Stages.Any(s => s.Number == stage))
            {
              var ruleTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == stage);
              var condition = ruleTransition == null ? null : ruleTransition.ConditionValue;
              var transition = Structures.ApprovalRuleCardReport.Transition.Create(stage, condition);
              variantSteps.Add(transition);
              if (ruleTransition == null)
              {
                steps.Add(variantSteps);
                variantSteps = new List<Structures.ApprovalRuleCardReport.Transition>();
                nextNumber = null;
                continue;
              }
            }
            else if (rule.Conditions.Any(s => s.Number == stage))
            {
              var ruleTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == stage && variant.Contains(t.TargetStage.Value));
              var currentStageIndex = variant.IndexOf(stage);
              if (currentStageIndex < variant.Count - 1)
              {
                var nextStage = variant[currentStageIndex + 1];
                ruleTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == stage && t.TargetStage.Value == nextStage);
              }
              
              bool? condition = null;
              if (ruleTransition != null)
              {
                condition = ruleTransition.ConditionValue;
              }
              else
              {
                var alternativeRuleTransition = rule.Transitions.FirstOrDefault(t => t.SourceStage == stage);
                if (alternativeRuleTransition != null)
                  condition = alternativeRuleTransition.ConditionValue == false;
              }
              
              var transition = Structures.ApprovalRuleCardReport.Transition.Create(stage, condition);
              variantSteps.Add(transition);
              if (ruleTransition == null)
              {
                steps.Add(variantSteps);
                variantSteps = new List<Structures.ApprovalRuleCardReport.Transition>();
                nextNumber = null;
                continue;
              }
            }
          }
        }
      }
      
      lastNumber = nextNumber;
      return steps;
    }
    
    private void AddStepsToRoute(IApprovalRuleBase rule,
                                 List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute,
                                 List<List<Structures.ApprovalRuleCardReport.Transition>> blocks)
    {
      var addedBlocks = new List<List<Structures.ApprovalRuleCardReport.Transition>>();
      var orderedBlocks = blocks.OrderByDescending(b => string.Join(string.Empty,
                                                                    b.Select(s => string.Format("{0}{1}",
                                                                                                s.SourceStage,
                                                                                                s.ConditionValue != false))));
      foreach (var block in orderedBlocks)
      {
        // Убрать дубли.
        var isDuplicate = false;
        foreach (var addedBlock in addedBlocks)
          isDuplicate = isDuplicate || block.SequenceEqual(addedBlock);

        if (isDuplicate)
          continue;
        
        // Условия.
        var conditionsBlocks = block.Where(b => rule.Conditions.Any(n => n.Number == b.SourceStage)).ToList();
        var hasCondition = conditionsBlocks.Any();
        if (hasCondition)
        {
          this.AddConditionsToRoute(rule, linedRoute, conditionsBlocks, Reports.Resources.ApprovalRuleCardReport.If, 1);
        }
        var beforeStagesLinesCount = linedRoute.Count;
        
        // Задания.
        var stagesBlocks = block.Where(b => rule.Stages.Any(n => n.Number == b.SourceStage));
        foreach (var stagesBlock in stagesBlocks)
        {
          var stage = rule.Stages.First(s => s.Number == stagesBlock.SourceStage);
          var level = hasCondition ? 2 : 1;
          Functions.ApprovalStageBase.AddStageToRoute(stage.StageBase, linedRoute, string.Empty, level);
        }
        
        // Нет заданий.
        if (hasCondition && beforeStagesLinesCount == linedRoute.Count)
        {
          var level = hasCondition ? 2 : 1;
          this.AddEmptyLine(linedRoute, level);
        }
        
        addedBlocks.Add(block);
      }
    }
    
    private void AddEmptyLine(List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute, int level)
    {
      var line = new Structures.ApprovalRuleCardReport.ConditionTableLine();
      line.Header = Reports.Resources.ApprovalRuleCardReport.EmptyString;
      var paddingLength = level * Constants.ApprovalRuleCardReport.LevelMultiplier;
      var padding = new string('\u2003', paddingLength);
      line.Header = string.Format("{0}{1}", padding, line.Header);
      linedRoute.Add(line);
    }
    
    private void AddConditionToRoute(IApprovalRuleBase rule,
                                     List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute,
                                     IConditionBase condition,
                                     bool direction,
                                     string prefix,
                                     int level)
    {
      var conditionInfo = new Structures.ApprovalRuleCardReport.ConditionTableLine();
      var conditionValue = direction ? Sungero.Docflow.Reports.Resources.ApprovalRuleCardReport.True : Sungero.Docflow.Reports.Resources.ApprovalRuleCardReport.False;
      var conditionName = string.Format("{0}{1}", Functions.ConditionBase.GetConditionName(condition), conditionValue);
      
      var header = string.Format("{0} {1}", prefix, conditionName);
      
      conditionInfo.Header = BreakLineAndAddPadding(header, Constants.ApprovalRuleCardReport.ConditionCellWidth, level);
      conditionInfo.Level = level;
      conditionInfo.IsCondition = true;
      
      linedRoute.Add(conditionInfo);
    }

    private void AddConditionsToRoute(IApprovalRuleBase rule,
                                      List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute,
                                      List<Structures.ApprovalRuleCardReport.Transition> conditions,
                                      string prefix,
                                      int level)
    {
      var conditionInfo = new Structures.ApprovalRuleCardReport.ConditionTableLine();
      var resources = Sungero.Docflow.Reports.Resources.ApprovalRuleCardReport;
      var conditionsNames = conditions.Select(b => string.Format("{0}{1}",
                                                                 Functions.ConditionBase.GetConditionName(rule.Conditions.First(s => s.Number == b.SourceStage).Condition),
                                                                 b.ConditionValue == true ? resources.True : resources.False));
      var conditionsNamesLabel = string.Join(string.Format("{0}{1}", System.Environment.NewLine, Reports.Resources.ApprovalRuleCardReport.And), conditionsNames);
      var header = string.Format("{0} {1}", prefix, conditionsNamesLabel);
      
      conditionInfo.Header = BreakLineAndAddPadding(header, Constants.ApprovalRuleCardReport.ConditionCellWidth, level);
      conditionInfo.Level = level;
      conditionInfo.IsCondition = true;
      
      linedRoute.Add(conditionInfo);
    }
    
    private IEnumerable<ISignatureSetting> GetSignatureSettings(IApprovalRuleBase rule)
    {
      var businessUnits = rule.BusinessUnits.Select(b => b.BusinessUnit).ToList();
      var documentKinds = rule.DocumentKinds.Select(d => d.DocumentKind).ToList();
      var ruleDepartments = rule.Departments.Select(d => d.Department).ToList();
      var categories = rule.DocumentGroups.Select(d => d.DocumentGroup).ToList();
      
      var defaultFilteredSignSettings = Functions.SignatureSetting.GetSignatureSettings(businessUnits, documentKinds)
        .Where(s => !s.Categories.Any() || !rule.DocumentGroups.Any() || s.Categories.Any(k => categories.Contains(k.Category)))
        .Where(s => s.DocumentFlow == rule.DocumentFlow || s.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.All);
      
      var signatureSettings = defaultFilteredSignSettings.ToList();
      foreach (var setting in defaultFilteredSignSettings)
      {
        // Дофильтровать по видам документов согласно документопотоку, кейс - баг 80143.
        if (!rule.DocumentKinds.Any() &&
            setting.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.All &&
            setting.DocumentKinds.Any())
        {
          var documentFlows = setting.DocumentKinds.Select(x => x.DocumentKind.DocumentFlow).Distinct();
          if (!documentFlows.Contains(rule.DocumentFlow))
            signatureSettings.Remove(setting);
        }
        
        // Дофильтровать по подразделениям отдельно, так как подразделений может быть огромное число и Contains() упадет.
        if (!ruleDepartments.Any() || !setting.Departments.Any())
          continue;
        
        var flag = false;
        var signatureSettingDepartments = setting.Departments.Select(sd => sd.Department).ToList();
        foreach (var signatureDepartment in signatureSettingDepartments)
        {
          if (ruleDepartments.Any(d => Equals(d, signatureDepartment)))
          {
            flag = true;
            break;
          }
        }
        
        if (!flag)
          signatureSettings.Remove(setting);
      }
      
      return signatureSettings;
    }

    /// <summary>
    /// Разбить строку на строки определенной длины и добавить отступы.
    /// </summary>
    /// <param name="line">Исходная строка.</param>
    /// <param name="cellWidth">Ширина ячейки.</param>
    /// <param name="level">Уровень отступа.</param>
    /// <returns>Разбитая строка.</returns>
    [Public]
    public static string BreakLineAndAddPadding(string line, int cellWidth, int level)
    {
      var paddingLength = level * Constants.ApprovalRuleCardReport.LevelMultiplier;
      var padding = new string('\u2003', paddingLength);
      var lineLengthLimit = cellWidth - (padding.Length * Constants.ApprovalRuleCardReport.SpaceLength);
      
      var lines = BreakLine(line, lineLengthLimit);
      
      return string.Join(System.Environment.NewLine, lines.Select(x => string.Format("{0}{1}", padding, x)));
    }
    
    private static IEnumerable<string> BreakLine(string line, int lineLengthLimit)
    {
      var result = new List<string>();
      char[] terminatorSymbols = { '\n', '\r' };
      
      while (line.Length > lineLengthLimit || line.IndexOfAny(terminatorSymbols) > 0)
      {
        var substringWithFullWords = string.Empty;
        var firstSubstringWithMaxLength = line.Length < lineLengthLimit ? line : line.Substring(0, lineLengthLimit);

        var lastTerminatorSymbolIndex = firstSubstringWithMaxLength.IndexOfAny(terminatorSymbols);
        if (lastTerminatorSymbolIndex > 0)
        {
          substringWithFullWords = firstSubstringWithMaxLength.Substring(0, lastTerminatorSymbolIndex + 1);
          line = line.Substring(lastTerminatorSymbolIndex + 2, line.Length - lastTerminatorSymbolIndex - 2);
        }
        else
        {
          var lastWhitespaceSymbolIndex = firstSubstringWithMaxLength.LastIndexOf(' ');
          if (lastWhitespaceSymbolIndex > 0)
          {
            substringWithFullWords = firstSubstringWithMaxLength.Substring(0, lastWhitespaceSymbolIndex + 1);
            line = line.Substring(lastWhitespaceSymbolIndex + 1, line.Length - lastWhitespaceSymbolIndex - 1);
          }
          else
          {
            substringWithFullWords = firstSubstringWithMaxLength;
            line = line.Substring(lineLengthLimit + 1, line.Length - lineLengthLimit - 1);
          }
        }

        result.Add(substringWithFullWords);
      }

      result.Add(line);
      return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.TrimStart());
    }
    
    #endregion
  }
}
