using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sungero.Commons.Structures.Module;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Commons.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить ссылки внешней системы, соответствующие заданным коду внешней системы и ИД сущности внешней системы.
    /// </summary>
    /// <param name="uuid">ИД сущности во внешней системе.</param>
    /// <param name="sysid">Код внешней системы.</param>
    /// <returns>Ссылки внешней системы.</returns>
    [Public]
    public IQueryable<IExternalEntityLink> GetExternalEntityLinks(string uuid, string sysid)
    {
      var result = ExternalEntityLinks.GetAll()
        .Where(x => x.ExtSystemId == sysid)
        .Where(x => x.ExtEntityId == uuid);
      return result;
    }
    
    /// <summary>
    /// Создать новый населенный пункт.
    /// </summary>
    /// <returns>Новый населенный пункт.</returns>
    [Remote]
    public static ICity CreateNewCity()
    {
      return Cities.Create();
    }
    
    /// <summary>
    /// Проверить, что для сущности все ExternalEntityLinks помечены IsDeleted.
    /// </summary>
    /// <param name="entity"> Сущность.</param>
    /// <returns>True, если так, иначе False.</returns>
    [Public]
    public static bool IsAllExternalEntityLinksDeleted(Sungero.Domain.Shared.IEntity entity)
    {
      var typeGuid = entity.TypeDiscriminator.ToString().ToUpper();
      var entityExternalLinks = ExternalEntityLinks.GetAll().Where(x => x.EntityType.ToUpper() == typeGuid &&
                                                                   x.EntityId == entity.Id ||
                                                                   x.ExtEntityId == entity.Id.ToString() &&
                                                                   x.ExtEntityType.ToUpper() == typeGuid);
      if (entityExternalLinks.Any(x => x.IsDeleted != true))
        return false;
      else
      {
        foreach (var link in entityExternalLinks)
        {
          ExternalEntityLinks.Delete(link);
        }
        return true;
      }
    }
    
    /// <summary>
    /// Проверить, что культура СП русская.
    /// </summary>
    /// <returns>True, если культура СП русская, иначе False.</returns>
    [Public]
    public static bool IsServerCultureRussian()
    {
      return Sungero.Core.TenantInfo.Culture.TwoLetterISOLanguageName.ToLower() == "ru";
    }
    
    /// <summary>
    /// Получить имя конечного типа сущности.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>Имя конечного типа сущности.</returns>
    [Public]
    public static string GetFinalTypeName(Sungero.Domain.Shared.IEntity entity)
    {
      var entityFinalType = entity.GetType().GetFinalType();
      var entityTypeMetadata = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(entityFinalType);
      return entityTypeMetadata.GetDisplayName();
    }
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Получить результаты предшествующего распознавания свойства сущности по факту, пришедшему из Ario.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="propertyName">Имя свойства, связанного с фактом.</param>
    /// <returns>Результаты последнего распознавания свойства сущности по факту, идентичному переданному.</returns>
    /// <remarks>Метод возвращает информацию о верификации данных пользователем.
    /// Подтвержденное пользователем значение находится в поле VerifiedValue IEntityRecognitionInfoFact.</remarks>
    [Public]
    public virtual IEntityRecognitionInfoFacts GetPreviousPropertyRecognitionResults(IArioFact fact,
                                                                                     string propertyName)
    {
      var factLabel = GetFactLabel(fact, propertyName);
      var recognitionInfo = EntityRecognitionInfos.GetAll()
        .Where(d => d.Facts.Any(f => f.FactLabel == factLabel && f.VerifiedValue != null && f.VerifiedValue != string.Empty))
        .OrderByDescending(d => d.Id)
        .FirstOrDefault();
      
      if (recognitionInfo == null)
        return null;
      
      return recognitionInfo.Facts
        .Where(f => f.FactLabel == factLabel && !string.IsNullOrWhiteSpace(f.VerifiedValue)).First();
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания свойства сущности по факту, пришедшему из Ario.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="propertyName">Имя свойства, связанного с фактом.</param>
    /// <param name="filterPropertyValue">Значение свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <param name="filterPropertyName">Имя свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <returns>Результаты последнего распознавания свойства сущности по факту, идентичному переданному.</returns>
    /// <remarks>Метод возвращает информацию о верификации данных пользователем.
    /// Подтвержденное пользователем значение находится в поле VerifiedValue IEntityRecognitionInfoFact.</remarks>
    [Public]
    public virtual IEntityRecognitionInfoFacts GetPreviousPropertyRecognitionResults(IArioFact fact,
                                                                                     string propertyName,
                                                                                     string filterPropertyValue,
                                                                                     string filterPropertyName)
    {
      var factLabel = GetFactLabel(fact, propertyName);
      var recognitionInfo = EntityRecognitionInfos.GetAll()
        .Where(d => d.Facts.Any(f => f.FactLabel == factLabel && f.VerifiedValue != null && f.VerifiedValue != string.Empty) &&
               d.Facts.Any(f => f.PropertyName == filterPropertyName && f.PropertyValue == filterPropertyValue))
        .OrderByDescending(d => d.Id)
        .FirstOrDefault();
      
      if (recognitionInfo == null)
        return null;
      
      return recognitionInfo.Facts
        .Where(f => f.FactLabel == factLabel && !string.IsNullOrWhiteSpace(f.VerifiedValue)).First();
    }
    
    #region Работа с фактами и полями

    /// <summary>
    /// Получить список фактов с переданным именем факта.
    /// </summary>
    /// <param name="facts">Факты.</param>
    /// <param name="factName">Имя факта.</param>
    /// <returns>Список фактов с искомым именем.</returns>
    [Public]
    public static List<IArioFact> GetFacts(List<IArioFact> facts, string factName)
    {
      return facts
        .Where(f => f.Name == factName)
        .ToList();
    }
    
    /// <summary>
    /// Получить список фактов с переданными именем факта и именем поля.
    /// </summary>
    /// <param name="facts">Факты.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Список фактов с искомыми именами факта и поля.</returns>
    [Public]
    public static List<IArioFact> GetFacts(List<IArioFact> facts, string factName, string fieldName)
    {
      return facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any(fl => fl.Name == fieldName))
        .ToList();
    }
    
    /// <summary>
    /// Получить метку факта.
    /// </summary>
    /// <param name="fact">Факт из Арио.</param>
    /// <param name="propertyName">Имя связанного свойства.</param>
    /// <returns>Метка факта.</returns>
    /// <remarks>Используется для быстрого поиска факта в результатах извлечения фактов.</remarks>
    [Public]
    public static string GetFactLabel(IArioFact fact, string propertyName)
    {
      string factInfo = fact.Name + propertyName;
      foreach (var field in fact.Fields)
        factInfo += field.Name + field.Value;
      
      var factHash = string.Empty;
      using (MD5 md5Hash = MD5.Create())
      {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(factInfo));
        for (int i = 0; i < data.Length; i++)
          factHash += data[i].ToString("x2");
      }
      return factHash;
    }
    
    /// <summary>
    /// Получить список фактов, отфильтрованный по имени факта и отсортированный по вероятности поля.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="orderFieldName">Имя поля, по вероятности которого будет произведена сортировка.</param>
    /// <returns>Отсортированный список фактов.</returns>
    /// <remarks>С учетом вероятности факта.</remarks>
    [Public]
    public static List<IArioFact> GetOrderedFacts(List<IArioFact> facts, string factName, string orderFieldName)
    {
      return facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any(fl => fl.Name == orderFieldName))
        .OrderByDescending(f => f.Fields.First(fl => fl.Name == orderFieldName).Probability)
        .ToList();
    }
    
    /// <summary>
    /// Получить поле из факта.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Поле из факта.</returns>
    [Public]
    public static IArioFactField GetField(IArioFact fact, string fieldName)
    {
      return GetFields(fact, new List<string>() { fieldName })
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить список полей из факта.
    /// </summary>
    /// <param name="fact">Имя факта.</param>
    /// <param name="fieldNames">Список имен поля.</param>
    /// <returns>Список полей.</returns>
    [Public]
    public static IQueryable<IArioFactField> GetFields(IArioFact fact, List<string> fieldNames)
    {
      if (fact == null)
        return null;
      return fact.Fields.Where(f => fieldNames.Contains(f.Name)).AsQueryable();
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям.
    /// </summary>
    /// <param name="weightedFields">Поля факта с весами.</param>
    /// <returns>Обобщенная вероятность.</returns>
    /// <remarks>Основана на формуле полной вероятности: P(A) = P(B1) x P(A|B1) + ... + P(Bn) x P(A|Bn).
    /// <para>Здесь:</para>
    /// <para>- P(Bi) - вероятность некоторого события/фактора;</para>
    /// <para>- P(A|Bi) - вероятность наступления события A в результате Bi.</para>
    /// <para></para>
    /// <para>В нашем случае:</para>
    /// <para>- А - насколько точную совокупную информацию несет факт;</para>
    /// <para>- Bi - вероятность конкретного поля факта;</para>
    /// <para>- P(A|Bi) - насколько поле значимо среди остальных полей факта.</para>
    /// <para></para>
    /// <para>Поля с пустыми Value исключаются из расчета. Нормализация полной вероятности применяется для защиты от:</para>
    /// <para>- отсутствующих полей;</para>
    /// <para>- полей с пустым Value.</para>
    /// <para>P(B1) x P(A|B1) + ... + P(Bk) x P(A|Bk)</para>
    /// <para>---------------------------------------</para>
    /// <para>        P(A|B1) + ... + P(A|Bk)        </para>
    /// </remarks>
    [Public]
    public static double GetAggregateFieldsProbability(System.Collections.Generic.IDictionary<IArioFactField, double> weightedFields)
    {
      // Сумма весов полей.
      var weightSum = 0.0;
      // Сумма произведений вероятностей непустых полей и их весов.
      var probabilitySum = 0.0;
      
      foreach (var weightedField in weightedFields)
      {
        // Если поле не имеет значения, то переходим к следующему.
        if (string.IsNullOrEmpty(weightedField.Key.Value))
          continue;
        
        weightSum += weightedField.Value;
        probabilitySum += weightedField.Key.Probability * weightedField.Value;
      }
      
      if (weightSum == 0)
        return 0.0;
      
      // Сумма весов полей в общем случае не равна 1.
      return probabilitySum / weightSum;
    }
    
    /// <summary>
    /// Получить значение поля из факта.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля.</returns>
    [Public]
    public static string GetFieldValue(IArioFact fact, string fieldName)
    {
      if (fact == null)
        return string.Empty;
      
      var field = fact.Fields.FirstOrDefault(f => f.Name == fieldName);
      if (field != null)
        return field.Value;
      
      return string.Empty;
    }

    /// <summary>
    /// Получить значение поля из фактов.
    /// </summary>
    /// <param name="facts"> Список фактов.</param>
    /// <param name="factName"> Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля, полученное из Ario с наибольшей вероятностью.</returns>
    [Public]
    public static string GetFieldValue(List<IArioFact> facts, string factName, string fieldName)
    {
      IEnumerable<IArioFactField> fields = facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any())
        .SelectMany(f => f.Fields);
      var field = fields
        .OrderByDescending(f => f.Probability)
        .FirstOrDefault(f => f.Name == fieldName);
      if (field != null)
        return field.Value;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить значение поля типа DateTime из фактов.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля типа DateTime.</returns>
    [Public]
    public static DateTime? GetFieldDateTimeValue(IArioFact fact, string fieldName)
    {
      var recognizedDate = GetFieldValue(fact, fieldName);
      if (string.IsNullOrWhiteSpace(recognizedDate))
        return null;
      
      DateTime date;
      if (Calendar.TryParseDate(recognizedDate, out date))
        return date;
      else
        return null;
    }

    /// <summary>
    /// Получить числовое значение поля из фактов.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Числовое значение поля.</returns>
    [Public]
    public static double? GetFieldNumericalValue(IArioFact fact, string fieldName)
    {
      var field = GetFieldValue(fact, fieldName);
      if (string.IsNullOrWhiteSpace(field))
        return null;

      double result;
      double.TryParse(field, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out result);
      return result;
    }
    
    /// <summary>
    /// Получить вероятность.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Вероятность.</returns>
    [Public]
    public static double? GetFieldProbability(IArioFact fact, string fieldName)
    {
      if (fact == null)
        return null;
      
      var field = fact.Fields.FirstOrDefault(f => f.Name == fieldName);
      if (field == null)
        return null;
      
      return field.Probability;
    }
    
    #endregion
    #endregion
  }
}