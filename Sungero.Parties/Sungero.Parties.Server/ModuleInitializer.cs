using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Parties.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateDefaultDueDiligenceWebsites();
      CreateDistributionListCounterparty();
      UpdateBanksFromCBR();
      CreateCounterpartyIndices();
    }
    
    /// <summary>
    /// Создать предопределенные сайты проверки контрагента.
    /// </summary>
    public static void CreateDefaultDueDiligenceWebsites()
    {
      InitializationLogger.Debug("Init: Create Default Due Diligence Websites");
      
      CreateDueDiligenceWebsite(Parties.Constants.DueDiligenceWebsite.Initialize.OwnerOnlineWebsite,
                                Parties.DueDiligenceWebsites.Resources.OwnerOnlineWebsiteName,
                                Constants.DueDiligenceWebsite.Websites.OwnerOnline.HomePage,
                                Constants.DueDiligenceWebsite.Websites.OwnerOnline.DueDiligencePage, false,
                                Parties.DueDiligenceWebsites.Resources.OwnerOnlineNote,
                                Constants.DueDiligenceWebsite.Websites.OwnerOnline.DueDiligencePage);
      CreateDueDiligenceWebsite(Parties.Constants.DueDiligenceWebsite.Initialize.ForFairBusinessWebsite,
                                Parties.DueDiligenceWebsites.Resources.ForFairBusinessWebsiteName,
                                Constants.DueDiligenceWebsite.Websites.ForFairBusiness.HomePage,
                                Constants.DueDiligenceWebsite.Websites.ForFairBusiness.DueDiligencePage, true,
                                Parties.DueDiligenceWebsites.Resources.ForFairBusinessNote,
                                Constants.DueDiligenceWebsite.Websites.ForFairBusiness.DueDiligencePageSE);
      CreateDueDiligenceWebsite(Parties.Constants.DueDiligenceWebsite.Initialize.HonestBusinessWebsite,
                                Parties.DueDiligenceWebsites.Resources.HonestBusinessWebsiteName,
                                Constants.DueDiligenceWebsite.Websites.HonestBusiness.HomePage,
                                Constants.DueDiligenceWebsite.Websites.HonestBusiness.DueDiligencePage, false,
                                Parties.DueDiligenceWebsites.Resources.HonestBusinessNote);
      CreateDueDiligenceWebsite(Parties.Constants.DueDiligenceWebsite.Initialize.KonturFocusWebsite,
                                Parties.DueDiligenceWebsites.Resources.KonturFocusWebsiteName,
                                Constants.DueDiligenceWebsite.Websites.KonturFocus.HomePage,
                                Constants.DueDiligenceWebsite.Websites.KonturFocus.DueDiligencePage, false,
                                Parties.DueDiligenceWebsites.Resources.KonturFocusNote);
      CreateDueDiligenceWebsite(Parties.Constants.DueDiligenceWebsite.Initialize.SbisWebsite,
                                Parties.DueDiligenceWebsites.Resources.SbisWebsiteName,
                                Constants.DueDiligenceWebsite.Websites.Sbis.HomePage,
                                Constants.DueDiligenceWebsite.Websites.Sbis.DueDiligencePage, false,
                                Parties.DueDiligenceWebsites.Resources.SbisNote);
    }
    
    /// <summary>
    /// Создать системного контрагента для рассылки нескольким адресатам.
    /// </summary>
    public static void CreateDistributionListCounterparty()
    {
      var needLink = false;
      var guid = Parties.Constants.Counterparty.DistributionListCounterpartyGuid;
      var name = Parties.Resources.DistributionListCounterpartyName;
      var company = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
      if (company == null)
      {
        company = Companies.Create();
        needLink = true;
      }
      
      company.Name = name;
      company.State.IsEnabled = false;
      company.Save();
      
      if (needLink)
        Docflow.PublicFunctions.Module.CreateExternalLink(company, guid);
    }
    
    /// <summary>
    /// Обновить банки.
    /// </summary>
    public static void UpdateBanksFromCBR()
    {
      if (Commons.PublicFunctions.Module.IsServerCultureRussian())
      {
        InitializationLogger.DebugFormat("Init: Update banks from CBR.");
        Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.PrepareBanksUpdate);
        Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.UpdateBanksFromCBR);
        var count = int.Parse(Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(Queries.Module.GetNewCountBanks).ToString());
        if (count > 0)
        {
          // BUG 75302, Zamerov: генерируем ID для новых банков кодом, иначе создание контрагентов через Create будет ставить дедлок.
          var tableName = Banks.Info.DBTableName;
          var ids = Domain.IdentifierGenerator.GenerateIdentifiers(tableName, count).ToList();
          using (var command = SQL.GetCurrentConnection().CreateCommand())
          {
            command.CommandText = Queries.Module.CreateBanksFromCBR;
            Docflow.PublicFunctions.Module.AddIntegerParameterToCommand(command, "@newId", ids.First());
            command.ExecuteNonQuery();
          }
        }
        Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.CleanTempTablesAfterUpdateBanks);
      }
    }
    
    /// <summary>
    /// Создать сайт проверки контрагента.
    /// </summary>
    /// <param name="entityId">GUID сайта.</param>
    /// <param name="name">Имя сайта.</param>
    /// <param name="homeUrl">Домашняя страница сайта.</param>
    /// <param name="url">Шаблон адреса сайта.</param>
    /// <param name="isDefault">Признак, что сайт используется при открытии сайта из карточки КА.</param>
    /// <param name="note">Примечание.</param>
    /// <param name="selfEmployedUrl">Шаблон адреса сайта (ИП) (по умолчанию - null).</param>
    public static void CreateDueDiligenceWebsite(Guid entityId, string name, string homeUrl, string url, bool isDefault, string note, string selfEmployedUrl = null)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(DueDiligenceWebsite.ClassTypeGuid, entityId);
      var dueDiligenceWebsite = DueDiligenceWebsites.Null;
      if (externalLink != null)
      {
        InitializationLogger.DebugFormat("Init: Refresh Due Diligence Website {0}", name);
        dueDiligenceWebsite = DueDiligenceWebsites.Get(externalLink.EntityId.Value);
      }
      else
      {
        InitializationLogger.DebugFormat("Init: Create Due Diligence Website {0}", name);
        dueDiligenceWebsite = DueDiligenceWebsites.Create();
        dueDiligenceWebsite.IsDefault = isDefault;
      }
      
      dueDiligenceWebsite.IsSystem = true;
      dueDiligenceWebsite.Name = name;
      dueDiligenceWebsite.HomeUrl = homeUrl;
      dueDiligenceWebsite.Url = url;
      if (selfEmployedUrl != null)
        dueDiligenceWebsite.UrlForSelfEmployed = selfEmployedUrl;
      dueDiligenceWebsite.Note = note;
      
      dueDiligenceWebsite.Save();
      
      if (externalLink == null)
        Docflow.PublicFunctions.Module.CreateExternalLink(dueDiligenceWebsite, entityId);
    }
    
    public static void CreateCounterpartyIndices()
    {
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.SungeroCounterpartyIndicesNameQuery);
      
      var tableName = Sungero.Parties.Constants.Module.SugeroCounterpartyTableName;
      var indexName = "idx_Counterparty_Discriminator_Status";
      var indexQuery = string.Format(Queries.Module.SungeroCounterpartyIndexQuery, tableName, indexName);
      Sungero.Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);      
    }
  }
}
