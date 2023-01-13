do $$
begin

-- Заполнить основание подписания, где тип основания - должностные обязанности.
update sungero_docflow_signsettings
set signingreason = 'Устав' where signingreason is null and reason = 'Duties';

-- Заполнить основание подписания, где тип основания - другой документ.
update sungero_docflow_signsettings
set signingreason = documentinfo where signingreason is null and reason = 'Other';

-- Заполнить основание подписания, где тип основания - доверенность.
-- Формируется как <Вид документа> №<номер> от <дата>.
update sungero_docflow_signsettings main
set signingreason = query.signingreason
from (select settings.id as id, 
             concat(substring(kind.shortname, 1, 183),
                    coalesce(' №' || edoc.regnumber_docflow_sungero, ''),
                    coalesce(' от ' || to_char(edoc.regdate_docflow_sungero, 'dd.mm.yyyy'), '')) as signingreason
      from sungero_docflow_signsettings settings
      join sungero_content_edoc edoc on settings.document = edoc.id
      join sungero_docflow_documentkind kind on edoc.documentkind_docflow_sungero = kind.id) as query
where query.id = main.id and main.signingreason is null and main.reason = 'PowerOfAttorney';

-- Заполнить отображаемое имя права подписи.
update sungero_docflow_signsettings
set name = concat('Должностные обязанности (', substring(signingreason, 1, 224), ')') where name is null and reason = 'Duties';

update sungero_docflow_signsettings
set name = signingreason where name is null and reason = 'PowerOfAttorney' or reason = 'Other';

end $$