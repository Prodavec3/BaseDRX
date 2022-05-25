-- Заполнить свойство Languages значением по умолчанию.
update sungero_docflow_smartsetting
set languages = 'rus; eng'
where languages is null;