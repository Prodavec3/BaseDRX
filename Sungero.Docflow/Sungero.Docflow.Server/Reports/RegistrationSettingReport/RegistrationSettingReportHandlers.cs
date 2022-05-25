using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.DatabookEntry;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow
{
  partial class RegistrationSettingReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(RegistrationSettingReport.SourceDataTableName, RegistrationSettingReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      RegistrationSettingReport.ReportDate = Calendar.UserToday;
      
      // Заполнить значения по умолчанию, если отчет вызван в невизуальном режиме.
      if (RegistrationSettingReport.FilterDepartmentsForBusinessUnits == null)
        RegistrationSettingReport.FilterDepartmentsForBusinessUnits = true;
      
      var filterDepartment = RegistrationSettingReport.FilterDepartmentsForBusinessUnits == true;

      #region Описание параметров
      
      if (RegistrationSettingReport.BusinessUnit != null)
        RegistrationSettingReport.ParamsDescriprion += string.Format("{0}: {1} \n", Reports.Resources.RegistrationSettingReport.BusinessUnit, RegistrationSettingReport.BusinessUnit.Name);
      
      if (!string.IsNullOrWhiteSpace(RegistrationSettingReport.Direction))
        RegistrationSettingReport.ParamsDescriprion += string.Format("{0}: {1} \n", Reports.Resources.RegistrationSettingReport.DocumentFlow, RegistrationSettingReport.DirectionLabel);
      
      RegistrationSettingReport.ParamsDescriprion += string.Format("{0}: {1} \n", Reports.Resources.RegistrationSettingReport.FilterDepartmentsForBusinessUnits,
                                                                   RegistrationSettingReport.FilterDepartmentsForBusinessUnits == true ? Reports.Resources.RegistrationSettingReport.Yes : Reports.Resources.RegistrationSettingReport.No);
      
      #endregion
      
      var sourceDataTableName = Constants.RegistrationSettingReport.SourceTableName;
      RegistrationSettingReport.SourceDataTableName = sourceDataTableName;
      var reportSessionId = System.Guid.NewGuid().ToString();
      RegistrationSettingReport.ReportSessionId = reportSessionId;
      
      var units = RegistrationSettingReport.BusinessUnit != null ?
        new List<Company.IBusinessUnit>() { RegistrationSettingReport.BusinessUnit } :
        Company.BusinessUnits.GetAll().Where(u => u.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      
      var flows = new List<Enumeration>() { DocumentFlow.Incoming, DocumentFlow.Outgoing, DocumentFlow.Inner, DocumentFlow.Contracts };
      if (!string.IsNullOrWhiteSpace(RegistrationSettingReport.Direction))
        flows = flows.Where(i => i.Value == RegistrationSettingReport.Direction).ToList();

      var kinds = DocumentKinds.GetAll().Where(k => k.Status == CoreEntities.DatabookEntry.Status.Active &&
                                               !Equals(k.NumberingType, DocumentKind.NumberingType.NotNumerable)).ToList();
      var settings = RegistrationSettings.GetAll().Where(s => s.Status == CoreEntities.DatabookEntry.Status.Active && s.DocumentRegister.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      var departments = Company.Departments.GetAll().Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active).OrderBy(d => d.Name).ToList();
      
      var separator = ";" + System.Environment.NewLine;
      var allDepartmentString = Reports.Resources.RegistrationSettingReport.AllDepartmentString;
      var otherDepartmentString = Reports.Resources.RegistrationSettingReport.OtherDepartmentString;
      // Строчки также используются в условной разметке для подсветки красным\курсивом.
      var settingNotFound = Reports.Resources.RegistrationSettingReport.SettingNotFound;
      var defaultSetting = Reports.Resources.RegistrationSettingReport.DefaultSetting;
      var registerNotFound = Reports.Resources.RegistrationSettingReport.RegisterNotFound;
      
      var tableData = new List<Structures.RegistrationSettingReport.TableLine>();

      foreach (var unit in units)
      {
        foreach (var flow in flows)
        {
          foreach (var kind in kinds.Where(k => Equals(k.DocumentFlow, flow)))
          {
            var error = Equals(kind.NumberingType, DocumentKind.NumberingType.Numerable) ? settingNotFound : defaultSetting;
            var subSettings = settings.Where(s => Equals(s.DocumentFlow, flow) &&
                                             (!s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, unit))) &&
                                             s.DocumentKinds.Any(k => Equals(k.DocumentKind, kind)))
              .ToList();
            
            foreach (var setting in subSettings)
            {
              var subDepartments = setting.Departments.Where(d => !filterDepartment || Equals(d.Department.BusinessUnit, unit)).ToList();
              if (setting.Departments.Any() && !subDepartments.Any())
                continue;

              var department = !setting.Departments.Any() ? allDepartmentString : string.Join(separator, subDepartments.Select(d => d.Department.Name));
              var example = Functions.DocumentRegister.GetValueExample(setting.DocumentRegister);

              var line = Structures.RegistrationSettingReport.TableLine.Create();
              line.BusinessUnit = unit.Name;
              line.DocumentFlow = GetDocumentFlowName(flow);
              line.DocumentFlowIndex = flows.IndexOf(flow);
              line.DocumentKind = kind.Name;
              line.RegistrationSetting = setting.Name;
              line.RegistrationSettingUri = Hyperlinks.Get(setting);
              line.Priority = setting.Priority ?? 0;
              line.Departments = department;
              line.SettingType = GetSettingType(kind, setting);
              line.DocumentRegister = setting.DocumentRegister.Name;
              line.DocumentRegisterUri = Hyperlinks.Get(setting.DocumentRegister);
              line.NumberExample = example;
              line.ReportSessionId = reportSessionId;

              tableData.Add(line);
            }
            
            // Если настроек для вида не найдено.
            if (!subSettings.Any() && kind.NumberingType == DocumentKind.NumberingType.Numerable)
            {
              var line = Structures.RegistrationSettingReport.TableLine.Create();
              line.BusinessUnit = unit.Name;
              line.DocumentFlow = GetDocumentFlowName(flow);
              line.DocumentFlowIndex = flows.IndexOf(flow);
              line.DocumentKind = kind.Name;
              line.RegistrationSetting = error;
              line.Priority = -1;
              line.Departments = allDepartmentString;
              line.SettingType = GetSettingType(kind, null);
              line.ReportSessionId = reportSessionId;
              
              tableData.Add(line);
              continue;
            }
            
            // Если есть хоть одна настройка для всех подразделений - идём дальше.
            if (subSettings.Any(s => !s.Departments.Any()))
              continue;
            // Если нет подразделений без настроек - идем дальше
            var allDepartmentsInUnit = departments.Where(d => Equals(d.BusinessUnit, unit)).ToList();
            var settingDepartments = subSettings.SelectMany(s => s.Departments).Select(s => s.Department).ToList();
            if (!allDepartmentsInUnit.Except(settingDepartments).Any())
              continue;
            
            var allDepartments = filterDepartment ?
              departments.Where(d => Equals(d.BusinessUnit, unit)).ToList() :
              departments;
            var registers = new List<IDocumentRegister>();
            var firstDepartment = allDepartments.Except(settingDepartments).FirstOrDefault();
            if (firstDepartment != null)
              registers.AddRange(Functions.DocumentRegister.GetDocumentRegistersByParams(kind, unit, firstDepartment, SettingType.Registration, false));
            registers = registers.Distinct().ToList();
            var regDepartment = allDepartments.Any(d => settingDepartments.Contains(d)) ? otherDepartmentString : allDepartmentString;
            
            if (!registers.Any())
            {
              var line = Structures.RegistrationSettingReport.TableLine.Create();
              line.BusinessUnit = unit.Name;
              line.DocumentFlow = GetDocumentFlowName(flow);
              line.DocumentFlowIndex = flows.IndexOf(flow);
              line.DocumentKind = kind.Name;
              line.RegistrationSetting = registerNotFound;
              line.Priority = -1;
              line.Departments = regDepartment;
              line.SettingType = GetSettingType(kind, null);
              line.ReportSessionId = reportSessionId;
              
              tableData.Add(line);
            }
            
            foreach (var register in registers)
            {
              var example = Functions.DocumentRegister.GetValueExample(register);
              
              var line = Structures.RegistrationSettingReport.TableLine.Create();
              line.BusinessUnit = unit.Name;
              line.DocumentFlow = GetDocumentFlowName(flow);
              line.DocumentFlowIndex = flows.IndexOf(flow);
              line.DocumentKind = kind.Name;
              line.RegistrationSetting = error;
              line.Priority = 0;
              line.Departments = regDepartment;
              line.DocumentRegister = register.Name;
              line.DocumentRegisterUri = Hyperlinks.Get(register);
              line.SettingType = GetSettingType(kind, null);
              line.NumberExample = example;
              line.ReportSessionId = reportSessionId;
              
              tableData.Add(line);
            }
          }
        }
      }
      
      // Полная сортировка данных.
      tableData = tableData
        .OrderBy(t => t.BusinessUnit)
        .ThenBy(t => t.DocumentFlowIndex)
        .ThenBy(t => t.DocumentKind)
        .ThenByDescending(t => t.Priority)
        .ThenBy(t => t.Departments)
        .ToList();
      
      for (var i = 0; i < tableData.Count; i++)
      {
        tableData[i].Id = i;
      }

      Functions.Module.WriteStructuresToTable(RegistrationSettingReport.SourceDataTableName, tableData);
    }
    
    private static string GetSettingType(IDocumentKind kind, IRegistrationSetting setting)
    {
      if (kind.AutoNumbering == true)
        return Reports.Resources.RegistrationSettingReport.AutoNumeration;
      
      if (setting != null)
        return RegistrationSettings.Info.Properties.SettingType.GetLocalizedValue(setting.SettingType);
      
      if (kind.NumberingType == DocumentKind.NumberingType.Registrable)
        return RegistrationSettings.Info.Properties.SettingType.GetLocalizedValue(SettingType.Registration);

      if (kind.NumberingType == DocumentKind.NumberingType.Numerable)
        return RegistrationSettings.Info.Properties.SettingType.GetLocalizedValue(SettingType.Numeration);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Заголовок группировки документопотока.
    /// </summary>
    /// <param name="flow">Документопоток.</param>
    /// <returns>Имя для группировки по документопотоку.</returns>
    private static string GetDocumentFlowName(Enumeration flow)
    {
      if (flow == ApprovalRuleBase.DocumentFlow.Inner)
        return Reports.Resources.RegistrationSettingReport.InternalDocuments;
      if (flow == ApprovalRuleBase.DocumentFlow.Incoming)
        return Reports.Resources.RegistrationSettingReport.IncomingDocuments;
      if (flow == ApprovalRuleBase.DocumentFlow.Outgoing)
        return Reports.Resources.RegistrationSettingReport.OutgoingDocuments;
      if (flow == ApprovalRuleBase.DocumentFlow.Contracts)
        return Reports.Resources.RegistrationSettingReport.ContractualDocuments;
      return null;
    }
  }
}