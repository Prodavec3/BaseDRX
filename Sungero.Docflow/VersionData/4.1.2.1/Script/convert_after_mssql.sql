-- Заполнить свойство Languages значением по умолчанию.
update Sungero_Docflow_SmartSetting
set Languages = 'rus; eng'
where Languages IS NULL