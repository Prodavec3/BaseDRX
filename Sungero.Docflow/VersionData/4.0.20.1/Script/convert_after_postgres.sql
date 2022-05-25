DO $$

-- Дискриминатор свойства-коллекции адресатов входящего письма (9EE5DA19-7A73-4C27-A743-EB68981C6EC2).
declare addresseesDiscriminator uuid = '9EE5DA19-7A73-4C27-A743-EB68981C6EC2';
declare addresseesNewId int;
declare documentToConvertCount int;
declare InAddresseesTableLastId int;

begin

-- Установить свойство IsManyAddresses в False.
update sungero_content_edoc
set inismanyaddr_docflow_sungero = 'false'
where discriminator = '8DD00491-8FD0-4A7A-9CF3-8B6DC2E6455D'
  and inismanyaddr_docflow_sungero is null;

-- Синхронизировать единичного адресата в коллекцию адресатов.
if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'sungero_docflow_inaddressees')
then

if exists(select * from information_schema.tables where table_name = 'temp_addresseestable')
then
  drop table temp_addresseestable;
end if;

create table temp_addresseestable
(
  Id serial,
  DocId int,
  AddresseeId int,
  DepartmentId int
);

insert into temp_addresseestable (DocId, AddresseeId, DepartmentId)
select distinct
  doc.id,
  doc.inaddressee_docflow_sungero,
  emp.department_company_sungero
from sungero_content_edoc as doc
  left join sungero_docflow_inaddressees addr on doc.id = addr.edoc
  left join sungero_core_recipient emp on doc.inaddressee_docflow_sungero = emp.id
where doc.discriminator = '8DD00491-8FD0-4A7A-9CF3-8B6DC2E6455D'
  and doc.inaddressee_docflow_sungero is not null
  and addr.edoc is null
order by doc.id;
  
-- Подсчет количества новых элементов.
select COUNT(DocId) into documentToConvertCount from temp_addresseestable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов входящего письма.
InAddresseesTableLastId := (select lastid from sungero_system_ids where tablename = 'sungero_docflow_inaddressees');

-- Резервирование id в таблице.
addresseesNewId := (select sungero_system_GetNewId('sungero_docflow_inaddressees', documentToConvertCount));

insert into sungero_docflow_inaddressees
(
  id,
  discriminator,
  edoc,
  number,
  addressee,
  department
)
select
  InAddresseesTableLastId + t.Id,
  addresseesDiscriminator,
  t.DocId,
  1,
  t.AddresseeId,
  t.DepartmentId
from temp_addresseestable as t;

drop table temp_addresseestable;

end if;

-- Заполнить ManyAddressesLabel у одноадресных входящих писем.
update sungero_content_edoc as doc
set manyaddrlabel_docflow_sungero = recip.name
from sungero_core_recipient as recip
where doc.inaddressee_docflow_sungero = recip.Id
  and doc.inismanyaddr_docflow_sungero = false
  and doc.inaddressee_docflow_sungero is not null;

-- Установить свойство IsAutoExecLeadingActionItem в False.
update sungero_docflow_personsetting
set ExecLeadAI = 'false'
where ExecLeadAI is null;

end $$;