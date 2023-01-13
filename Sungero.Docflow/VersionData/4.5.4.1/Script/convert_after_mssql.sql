-- Заполнить основание подписания, где тип основания - должностные обязанности.
update Sungero_Docflow_SignSettings
set SigningReason = 'Устав' where SigningReason is null and Reason = 'Duties'

-- Заполнить основание подписания, где тип основания - другой документ.
update Sungero_Docflow_SignSettings
set SigningReason = DocumentInfo where SigningReason is null and Reason = 'Other'

-- Заполнить основание подписания, где тип основания - доверенность.
-- Формируется как <Вид документа> №<номер> от <дата>.
update main
set SigningReason = query.SigningReason
from (select settings.Id as Id,
             substring(kind.ShortName, 1, 183) +
               COALESCE(' №' + edoc.RegNumber_Docflow_Sungero, '') + 
               COALESCE(' от ' + CONVERT(varchar, edoc.RegDate_Docflow_Sungero, 104), '') as SigningReason
      from Sungero_Docflow_SignSettings settings
      join Sungero_Content_EDoc edoc on settings.Document = edoc.Id
      join Sungero_Docflow_DocumentKind kind on edoc.DocumentKind_Docflow_Sungero = kind.Id) as query, Sungero_Docflow_SignSettings main
where query.Id = main.Id and main.SigningReason is null and main.Reason = 'PowerOfAttorney'

-- Заполнить отображаемое имя права подписи.
update Sungero_Docflow_SignSettings
set Name = 'Должностные обязанности (' + substring(SigningReason, 1, 224) + ')' where Name is null and Reason = 'Duties'

update Sungero_Docflow_SignSettings
set Name = SigningReason where Name is null and Reason = 'PowerOfAttorney' or Reason = 'Other'