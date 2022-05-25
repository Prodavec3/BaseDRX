using System;

namespace Sungero.Projects.Constants
{
  public static class Module
  {

    #region Выдача прав на проектные документы

    // Идентификатор фонового процесса выдачи прав на проектные документы.
    public const string LastProjectDocumentRightsUpdateDate = "LastProjectDocumentRightsUpdateDate";
    
    public const string LastProjectRightsUpdateDate = "LastProjectRightsUpdateDate";
    
    public const string AccessRightsReadTypeName = "Read";
    
    public const string AccessRightsEditTypeName = "Edit";
    
    public const string AccessRightsFullAccessTypeName = "FullAccess";
    
    #endregion
    
    #region Папки проектов

    public static class ProjectFolders
    {
      // UID для корневой папки с проектами.
      public static readonly Guid ProjectFolderUid = Guid.Parse("F7A78196-A1BE-4666-94F4-0DDDD3367A6E");
      
      // UID для корневой папки с проектами.
      public static readonly Guid ProjectArhiveFolderUid = Guid.Parse("C74412F4-7FBB-450F-83A3-BBB9772C6167");
    }
    
    #endregion

    #region Группы, роли, тип прав
    
    public static class RoleGuid
    {
      // GUID роли "Проектные команды".
      [Sungero.Core.Public]
      public static readonly Guid ParentProjectTeam = Guid.Parse("2062682D-745C-4E02-AF2F-26AD229E8C61");
    }
    
    #endregion
    
    public static class Initialize
    {
      [Sungero.Core.Public]
      public static readonly Guid CustomerRequirementsKind = Guid.Parse("FC5B2F85-548D-4DE0-B1E9-66C873932111");
      [Sungero.Core.Public]
      public static readonly Guid RegulationsKind = Guid.Parse("1B1F18B1-F42E-4939-B6ED-14D555D5FAAA");
      [Sungero.Core.Public]
      public static readonly Guid ReportKind = Guid.Parse("D0859D72-15C7-4CA7-B81E-611A5DF1F112");
      [Sungero.Core.Public]
      public static readonly Guid ScheduleKind = Guid.Parse("F5869F19-67D3-47C2-9024-C60A96F2685B");
      [Sungero.Core.Public]
      public static readonly Guid ProjectSolutionKind = Guid.Parse("479B7A76-3F38-434C-897E-733198C4F260");
      [Sungero.Core.Public]
      public static readonly Guid AnalyticNoteKind = Guid.Parse("205D0822-EEAB-4EB9-813D-738ECEEFE303");
      
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindInvestment = Guid.Parse("38DE9EBF-8733-41B9-88B5-8C1884075E2C");
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindInformationTechnology = Guid.Parse("2C569F84-709F-40C8-A179-97D58037B6A8");
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindOrganizationDevelopment = Guid.Parse("F4BC2B22-7E28-4EED-B9A9-8A8DAB275DA4");
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindCreatingNewProduct = Guid.Parse("D3568B17-0FA1-4D29-8ABB-046A8DDE4796");
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindOrganizationSale = Guid.Parse("53FBC9CF-726C-45AE-BD77-8C9BFEC2A0C0");
      [Sungero.Core.Public]
      public static readonly Guid ProjectKindMarketing = Guid.Parse("04FF23F3-33A5-4EAE-A6B7-6398A71D0866");
    }

    public const string DontUpdateModified = "DontUpdateModified";
  }
}