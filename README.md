Fitness Center API — Отчёт 1 (in-memory)
REST API для управления фитнес-центром. Данные хранятся в памяти процесса (in-memory, Map).
Во второй части хранилище заменяется на реальную СУБД без изменения контроллеров и маршрутов.

# Технологии

| Компоненты | Решение |
| ------------- | ------------- |
| Язык | C# |
| Хранилище | In-memory (Map) |



# Модели данных 

Client 

| Поле | Тип | Обязательно | Описание |
| ------------- | ------------- | ------------- | ------------- |
| id | UUID | генерируется | Уникальный идентификатор |
| surname | String | ✅ | Фамилия |
| name | String | ✅ | Имя |
| patronymic | String | ❌ | Отчество (null если не указано) |
| birthday | Date | ✅ | Дата рождения (YYYY-MM-DD) |
| phone | String | ✅ | Номер телефона |
| email | String | ✅ | Email |
| is_active | Boolean | по стан. true | Активен ли клиент |
| trainer_id | UUID | ❌ | Ссылка на тренера |

Traner

| Поле | Тип | Обязательно | Описание |
| ------------- | ------------- | ------------- | ------------- |
| id | UUID | генерируется | Уникальный идентификатор |
| surname | String | ✅ | Фамилия |
| name | String | ✅ | Имя |
| patronymic | String | ❌ | Отчество |
| phone | String | ✅ | Номер телефона |
| status | Enum | по умолч. WORKING | WORKING / ON_LEAVE / NOT_WORKING |

REST API
Базовый путь: /api/


### Клиенты `/api/clients`

#### `POST /api/clients` — создать клиента
**Body:**
```json
{
  "surname": "Петров",
  "name": "Пётр",
  "patronymic": "Петрович",
  "birthday": "1995-03-20",
  "phone": "+79161234567",
  "email": "petr@example.com",
  "trainer_id": "uuid-тренера"
}
```
**Ответ:** `201 Created`
```json
{
  "id": "...",
  "surname": "Петров",
  "name": "Пётр",
  "patronymic": "Петрович",
  "birthday": "1995-03-20",
  "phone": "+79161234567",
  "email": "petr@example.com",
  "is_active": true,
  "trainer_id": "uuid-тренера"
}
```
---

#### `GET /api/clients` — список всех клиентов
**Ответ:** `200 OK` — массив клиентов

---

#### `GET /api/clients/:id` — краткая информация о клиенте
**Ответ:** `200 OK` или `404`

---

#### `GET /api/clients/:id/detail` — подробная информация (с объектом тренера)
**Ответ:** `200 OK`
```json
{
  "id": "...",
  "name": "Иван",
  "trainer": {
    "id": "...",
    "name": "Алексей",
    "status": "WORKING"
  }
}
```

---

#### `PUT /api/clients/:id` — обновить данные клиента
**Body:** любые поля клиента кроме id, is_active, trainer_id  
**Ответ:** `200 OK`

---

#### `PATCH /api/clients/:id/status` — активировать / деактивировать
**Body:**
```json
{ "is_active": false }
```
**Ответ:** `200 OK`

---

#### `POST /api/clients/:clientId/trainer/:trainerId` — назначить тренера
**Ответ:** `200 OK` — обновлённый клиент

---

### Тренеры `/api/trainers`

#### `POST /api/trainers` — создать тренера
**Body:**
```json
{
  "surname": "Новиков",
  "name": "Сергей",
  "phone": "+79260001122",
  "status": "WORKING"
}
```
**Ответ:** `201 Created`

---

#### `GET /api/trainers` — список всех тренеров
**Ответ:** `200 OK`

---

#### `GET /api/trainers/:id/detail` — подробная информация (со списком клиентов)
**Ответ:** `200 OK`
```json
{
  "id": "...",
  "name": "Алексей",
  "status": "WORKING",
  "clients": [ { ... }, { ... } ]
}
```

---

#### `PUT /api/trainers/:id` — обновить данные тренера
**Ответ:** `200 OK`

---

#### `PATCH /api/trainers/:id/status` — изменить статус
**Body:**
```json
{ "status": "ON_LEAVE" }
```
**Допустимые значения:** `WORKING`, `ON_LEAVE`, `NOT_WORKING`  
**Ответ:** `200 OK`

---

## Коды ответов

| Код | Ситуация                                        |
|-----|-------------------------------------------------|
| 200 | Успешное чтение или обновление                  |
| 201 | Успешное создание ресурса                       |
| 400 | Ошибка валидации или бизнес-ошибка              |
| 404 | Ресурс не найден                                |
| 500 | Внутренняя ошибка сервера                       |

### Формат ошибки 400 (валидация)
```json
{
  "error": "Ошибка валидации",
  "details": [
    { "field": "email", "message": "Некорректный email" },
    { "field": "birthday", "message": "birthday обязателен" }
  ]
}
```

### Формат ошибки 404
```json
{ "error": "Клиент не найден" }
```

---
