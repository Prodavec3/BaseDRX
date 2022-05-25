using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties.Server
{
  partial class BankFunctions
  {
    /// <summary>
    /// Получить банки с одинаковым БИК.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые.</param>
    /// <returns>Банки с одинаковым БИК.</returns>
    [Public, Remote]
    public List<IBank> GetBanksWithSameBic(bool excludeClosed)
    {
      var banks = Banks.GetAll();
      
      // Отфильтровать закрытые сущности.
      if (excludeClosed)
        banks = banks.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      return banks.Where(b => b.BIC == _obj.BIC && b.Id != _obj.Id).ToList();
    }
    
    /// <summary>
    /// Получить банки с одинаковым SWIFT.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые.</param>
    /// <returns>Банки с одинаковым SWIFT.</returns>
    [Public, Remote]
    public List<IBank> GetBanksWithSameSwift(bool excludeClosed)
    {
      var banks = Banks.GetAll();
      
      // Отфильтровать закрытые сущности.
      if (excludeClosed)
        banks = banks.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      return banks.Where(b => b.SWIFT == _obj.SWIFT && b.Id != _obj.Id).ToList();
    }
    
    /// <summary>
    /// Банки, участвующие в договорах, доп. соглашениях.
    /// </summary>
    /// <returns>Список ИД банков.</returns>
    [Remote(IsPure = true)]
    public static List<int> GetBankIdsServer()
    {
      var supAgrBankIds = Sungero.Contracts.SupAgreements.GetAll(r => Banks.Is(r.Counterparty)).Select(p => p.Counterparty.Id).ToList();
      var contractBankIds = Sungero.Contracts.Contracts.GetAll(r => Banks.Is(r.Counterparty)).Select(p => p.Counterparty.Id).ToList();
      var result = new List<int>();
      result.AddRange(supAgrBankIds);
      result.AddRange(contractBankIds);
      return result.ToList();
    }
  }
}