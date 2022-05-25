using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationGroup;

namespace Sungero.Docflow.Server
{
  partial class RegistrationGroupFunctions
  {

    /// <summary>
    /// Получить журналы группы.
    /// </summary>
    /// <returns>Журналы регистрации.</returns>
    [Remote]
    public IQueryable<IDocumentRegister> GetGroupDocumentRegisters()
    {
      return DocumentRegisters.GetAll()
        .Where(a => Equals(a.RegistrationGroup, _obj))
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active);
    }
    
    /// <summary>
    /// Проверить журналы группы регистрации на возможность их регистрации.
    /// </summary>
    /// <returns>Текст хинта, с указанием необходимых документопотоков.</returns>
    public string ValidateDocumentFlow()
    {
      var documentRegisters = Functions.RegistrationGroup.GetGroupDocumentRegisters(_obj);
      var flows = new List<string>();
      
      if (_obj.CanRegisterIncoming != true && documentRegisters.Any(r => r.DocumentFlow == Docflow.DocumentRegister.DocumentFlow.Incoming))
        flows.Add(RegistrationGroups.Resources.Incoming);
      
      if (_obj.CanRegisterInternal != true && documentRegisters.Any(r => r.DocumentFlow == Docflow.DocumentRegister.DocumentFlow.Inner))
        flows.Add(RegistrationGroups.Resources.Inner);
      
      if (_obj.CanRegisterOutgoing != true && documentRegisters.Any(r => r.DocumentFlow == Docflow.DocumentRegister.DocumentFlow.Outgoing))
        flows.Add(RegistrationGroups.Resources.Outgoing);
      
      if (_obj.CanRegisterContractual != true && documentRegisters.Any(r => r.DocumentFlow == Docflow.DocumentRegister.DocumentFlow.Contracts))
        flows.Add(RegistrationGroups.Resources.Contractual);
      
      return flows.Count == 0 ? string.Empty : RegistrationGroups.Resources.IncorrectDocumentFlowFormat(string.Join(", ", flows));
    }

    /// <summary>
    /// Выдаем права на типы документов согласно прокликанным галочкам.
    /// Выдавать права может только администратор.
    /// </summary>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Obsolete("Используйте метод GrantRegistrationAccessRights.")]
    public virtual void GrantRegistrationRights()
    {
      if (Users.Current.IncludedIn(Roles.Administrators))
      {
        var incomingRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          IncomingDocumentBases.AccessRights,
          FinancialArchive.IncomingTaxInvoices.AccessRights
        };
        
        var outgoingRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          OutgoingDocumentBases.AccessRights,
          FinancialArchive.OutgoingTaxInvoices.AccessRights
        };
        
        var internalRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          InternalDocumentBases.AccessRights
        };
        
        var contractRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          ContractualDocumentBases.AccessRights,
          FinancialArchive.ContractStatements.AccessRights,
          FinancialArchive.UniversalTransferDocuments.AccessRights,
          FinancialArchive.Waybills.AccessRights
        };
        
        this.GrantAccessRightsOnTypes(_obj.CanRegisterIncoming == true, incomingRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterOutgoing == true, outgoingRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterInternal == true, internalRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterContractual == true, contractRights);
      }
    }

    /// <summary>
    /// Выдаем права на типы документов согласно прокликанным галочкам.
    /// Выдавать права может только администратор.
    /// </summary>
    public virtual void GrantRegistrationAccessRights()
    {
      if (Users.Current.IncludedIn(Roles.Administrators))
      {
        var incomingRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          IncomingDocumentBases.AccessRights,
          FinancialArchive.IncomingTaxInvoices.AccessRights
        };
        
        var outgoingRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          OutgoingDocumentBases.AccessRights,
          FinancialArchive.OutgoingTaxInvoices.AccessRights,
          Contracts.OutgoingInvoices.AccessRights
        };
        
        var internalRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          InternalDocumentBases.AccessRights
        };
        
        var contractRights = new List<Domain.Shared.IEntityAccessRights>()
        {
          ContractualDocumentBases.AccessRights,
          FinancialArchive.ContractStatements.AccessRights,
          FinancialArchive.UniversalTransferDocuments.AccessRights,
          FinancialArchive.Waybills.AccessRights
        };
        
        this.GrantAccessRightsOnTypes(_obj.CanRegisterIncoming == true, incomingRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterOutgoing == true, outgoingRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterInternal == true, internalRights);
        this.GrantAccessRightsOnTypes(_obj.CanRegisterContractual == true, contractRights);
      }
    }
    
    /// <summary>
    /// Выдать права на регистрацию.
    /// </summary>
    /// <param name="grant">True, если выдать права, false, если изъять.</param>
    /// <param name="types">Список прав доступа для типов и экземпляров сущности.</param>
    protected void GrantAccessRightsOnTypes(bool grant, List<Domain.Shared.IEntityAccessRights> types)
    {
      var registration = Constants.Module.DefaultAccessRightsTypeSid.Register;

      if (grant)
      {
        foreach (var rights in types)
          rights.Grant(_obj, registration);
      }
      else
      {
        foreach (var rights in types)
          rights.Revoke(_obj, registration);
      }
      foreach (var rights in types)
        rights.Save();
    }
  }
}