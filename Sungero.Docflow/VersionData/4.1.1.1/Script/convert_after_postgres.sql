DO $$

-- Дискриминатор свойства-коллекции адресатов служебной записки (5c815151-6faf-4b26-a47a-bd82f68c0c23).
declare addresseesDiscriminator uuid = '5c815151-6faf-4b26-a47a-bd82f68c0c23';
declare addresseesNewId int;
declare documentToConvertCount int;
declare memoAddresseesTableLastId int;

begin

-- Установить свойство IsManyAddressees в False.
update sungero_content_edoc
set memoismanyaddr_docflow_sungero = 'false'
where discriminator = '95af409b-83fe-4697-a805-5a86ceec33f5'
  and memoismanyaddr_docflow_sungero is null;

-- Синхронизировать единичного адресата в коллекцию адресатов.
if exists(select 1 from information_schema.tables where table_name = 'sungero_docflow_memoaddressees')
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
  doc.addressee_docflow_sungero,
  emp.department_company_sungero
from sungero_content_edoc as doc
  left join sungero_docflow_memoaddressees addr on doc.id = addr.edoc
  left join sungero_core_recipient emp on doc.addressee_docflow_sungero = emp.id
where doc.discriminator = '95af409b-83fe-4697-a805-5a86ceec33f5'
  and doc.addressee_docflow_sungero is not null
  and addr.edoc is null
order by doc.id;
  
-- Подсчет количества новых элементов.
select COUNT(DocId) into documentToConvertCount from temp_addresseestable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов служебной записки.
memoAddresseesTableLastId := (select lastid from sungero_system_ids where tablename = 'sungero_docflow_memoaddressees');

-- Резервирование id в таблице.
addresseesNewId := (select sungero_system_GetNewId('sungero_docflow_memoaddressees', documentToConvertCount));

insert into sungero_docflow_memoaddressees
(
  id,
  discriminator,
  edoc,
  number,
  addressee,
  department
)
select
  memoAddresseesTableLastId + t.Id,
  addresseesDiscriminator,
  t.DocId,
  1,
  t.AddresseeId,
  t.DepartmentId
from temp_addresseestable as t;

drop table temp_addresseestable;

end if;

-- Заполнить ManyAddresseesLabel у одноадресных служебных записок.
update sungero_content_edoc as doc
set memomanyaddrlb_docflow_sungero = recip.name
from sungero_core_recipient as recip
where doc.addressee_docflow_sungero = recip.Id
  and doc.memoismanyaddr_docflow_sungero = false
  and doc.addressee_docflow_sungero is not null
  and doc.memomanyaddrlb_docflow_sungero is null;

-- Заполнение поля StageBase из значения поля Stage.
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_rulestages' and column_name = 'stagebase')
then
update public.sungero_docflow_rulestages
set stagebase = stage
where stagebase is null; 
end if;

end $$;