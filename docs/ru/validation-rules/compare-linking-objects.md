﻿# Сравнение объектов привязки

Алгоритм сравнения объектов привязки - не универсальный.

У позиции номенклатуры есть поле "тип учёта объекта привязки".


Операция сравнения определена для следующих типов:

- Рубрика
- Адрес фирмы
- Рубрика + адреса фирмы
- Рубрика 1 уровня + адрес фирмы

Для остальных типов учёта операция сравнения объектов привязки не определена, сравниваются только номенклатурные позиции по ID.

Если сравниваются две позиции, у которых тип учёта объекта привязки совпадает ("Рубрика" - "Рубрика"), то сравнение тривиально: нужно сравнивать одинаковые части объекта привязки.
Неопределённость начинается, если сравнивать позиции с **разным типом учёта объекта привязки**.

Как сравнить позицию с учётом "Адрес фирмы" с позицией с учётом "Рубрика" ?

Ответ даёт следующая таблица

| Тип учёта объекта привязки              | Рубрика (c3) | Рубрика + адреса фирмы (c3 & a) | Рубрика 1 уровня + адрес фирмы (c1 & a) | Адрес фирмы (a) | None | 
|-----------------------------------------|--------------|---------------------------------|-----------------------------------------|-----------------|------|
| Рубрика (c3)                            | c3           | c3                              | -                                       | -               | -    |
| Рубрика + адреса фирмы (c3 & a)         | c3           | c3 & a                          | -                                       | a               | -    |
| Рубрика 1 уровня + адрес фирмы (c1 & a) | -            | -                               | c1 & a                                  | a               | -    |
| Адрес фирмы (a)                         | -            | a                               | a                                       | a               | -    |
| None                                    | -            | -                               | -                                       | -               | равны всегда |

Здесь 
* просто символ, например c1 означет, что соответствующие поля имею значения и равны. 
* отсутствие символа (например, смвола a в "c1 & c3") означет, что соответствующее поле хотя-бы в одном объекте отсутствует.
* "-" означает, что объекты различаются без дополнительных условий.

# Типы учёта объекта привязки
На самом деле в ERM типов учёта объекта привязки больше, но не все они используются для проверки на сопутствующие\запрещённые позиции.
В проекте river целесообразно провести группировку типов:

| Тип учёта River | Тип учёта ERM |
|---------------------------|--------------------------------------------------------------------|
| Category | CategorySingle, CategoryMultiple, CategoryMultipleAsterix |
| Address | AddressSingle, AddressMultiple |
| AddressCategory | AddressCategorySingle, AddressCategoryMultiple |
| AddressFirstLevelCategory | AddressFirstLevelCategorySingle, AddressFirstLevelCategoryMultiple |
| None | все остальные типы учёта |

Для типа None в River не определена операция сравнения объектов привязки, сравнение производится только по ID позиций номенклатуры.

PS. В Erm сравнение реализовано несимметрино в паре "Адрес фирмы" - "Рубрика 1 уровня + адрес фирмы (c1 & a)": в одном направлении сравнение выполняется по адресу, как у нас, в другом сравнение не выполняется и объекты привязки всегда считаются различными.
