using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Агент синхронизации контрагентов.
    /// </summary>
    public virtual void SyncCounterparties()
    {
      var boxes = Functions.BusinessUnitBox.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        Functions.BusinessUnitBox.SyncBoxCounterparties(box);
      }
    }
    
    /// <summary>
    /// Агент синхронизации ящиков.
    /// </summary>
    public virtual void SyncBoxes()
    {
      var allBoxes = BusinessUnitBoxes.GetAll().ToList();
      var boxes = allBoxes.Where(b => Equals(b.ConnectionStatus, ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected) ||
                                 Equals(b.ConnectionStatus, ExchangeCore.BusinessUnitBox.ConnectionStatus.Error)).ToList();
      foreach (var box in boxes)
      {
        Transactions.Execute(() =>
                             {
                               Functions.BusinessUnitBox.SyncBoxStatus(box);
                               
                               if (box.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
                               {
                                 Functions.BusinessUnitBox.UpdateExchangeServiceCertificates(box);
                                 Functions.DepartmentBox.CreateDepartmentBoxes(box);
                               }
                             });
      }
      // Проставить статус соединения в абонентских ящиках подразделений.
      foreach (var box in allBoxes)
        Functions.Module.SetDepartmentBoxConnectionStatus(box);
      
      var boxIds = boxes.Select(b => b.Id).ToList();
      boxes = BusinessUnitBoxes.GetAll().Where(b => boxIds.Contains(b.Id)).ToList();
      foreach (var box in boxes)
        Functions.BusinessUnitBox.ValidateCertificates(box);
    }
  }
}