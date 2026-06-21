using Microsoft.Data.Sqlite;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString = "Data Source=database.db";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    using var createCommand = new SqliteCommand(@"
        CREATE TABLE IF NOT EXISTS clients (
            id TEXT NOT NULL PRIMARY KEY,
            surname TEXT NOT NULL,
            name TEXT NOT NULL,
            patronymic TEXT,
            birthday TEXT NOT NULL,
            phone TEXT NOT NULL,
            email TEXT NOT NULL,
            is_active INTEGER NOT NULL DEFAULT 0 CHECK (is_active IN (0, 1)),
            trainer_id TEXT,
            locker_id TEXT,
            FOREIGN KEY (trainer_id) REFERENCES trainers(id),
            FOREIGN KEY (locker_id) REFERENCES lockers(id)
        );
        CREATE TABLE IF NOT EXISTS trainers (
            id TEXT NOT NULL PRIMARY KEY,
            surname TEXT NOT NULL,
            name TEXT NOT NULL,
            patronymic TEXT,
            phone TEXT NOT NULL,
            status INTEGER NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2))
        );
        CREATE TABLE IF NOT EXISTS lockers (
            id TEXT NOT NULL PRIMARY KEY,
            number INTEGER NOT NULL CHECK (number BETWEEN 1 AND 20),
            client_id TEXT,
            FOREIGN KEY (client_id) REFERENCES clients(id)
        );
        CREATE TABLE IF NOT EXISTS services (
            id TEXT NOT NULL PRIMARY KEY,
            name TEXT NOT NULL,
            price INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS client_services (
            client_id TEXT NOT NULL,
            service_id TEXT NOT NULL
        );
        INSERT OR IGNORE INTO services (id, name, price) VALUES
        (""SOLARIUM"", ""Солярий"", 400),
        (""POOL"", ""Бассейн"", 200),
        (""SAUNA"", ""Сауна"", 0),
        (""CRYOSAUNA"", ""Криосауна"", 1000),
        (""CROSSFIT"", ""Кроссфит"", 500);
    ", connection);
    createCommand.ExecuteNonQuery();


    using var lockersCommand = new SqliteCommand("SELECT COUNT(*) FROM lockers;", connection);
    long count = (long)lockersCommand.ExecuteScalar() + 1;

    for (long i = count; i <= 20; i++) {
        using var insertCommand = new SqliteCommand("INSERT INTO lockers (id, number) VALUES ($id, $number);", connection);
        insertCommand.Parameters.AddWithValue("$id", Guid.NewGuid());
        insertCommand.Parameters.AddWithValue("$number", i);
        insertCommand.ExecuteNonQuery();
    }
}

app.MapPost("/api/clients", (Client client) => {
    client.id = Guid.NewGuid();
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();

        using (var command = new SqliteCommand(@"
            INSERT INTO clients (id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id) VALUES
            ($id, $surname, $name, $patronymic, $birthday, $phone, $email, $is_active, $trainer_id, $locker_id);
        ", connection))
        {
            command.Parameters.AddWithValue("$id", client.id);
            command.Parameters.AddWithValue("$surname", client.surname);
            command.Parameters.AddWithValue("$name", client.name);
            command.Parameters.AddWithValue("$patronymic", client.patronymic is null ? (object)DBNull.Value : client.patronymic);
            command.Parameters.AddWithValue("$birthday", client.birthday);
            command.Parameters.AddWithValue("$phone", client.phone);
            command.Parameters.AddWithValue("$email", client.email);
            command.Parameters.AddWithValue("$is_active", client.is_active);
            command.Parameters.AddWithValue("$trainer_id", client.trainer_id.HasValue ? client.trainer_id : (object)DBNull.Value);
            command.Parameters.AddWithValue("$locker_id", client.locker_id.HasValue ? client.locker_id : (object)DBNull.Value);
            command.ExecuteNonQuery();
        }
    }
    return client;
});
app.MapPut("/api/clients/{id}", (string id, Client client) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var existsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM clients WHERE id = $id);", connection);
    existsCommand.Parameters.AddWithValue("$id", id.ToUpper());
    var exists = (long)existsCommand.ExecuteScalar();
    if (exists != 1) return Results.NotFound();
    else
    {
        var insertcommand = new SqliteCommand(@"
            UPDATE clients
            SET surname = $surname,
                name = $name,
                patronymic = $patronymic,
                birthday = $birthday,
                phone = $phone,
                email = $email,
                is_active = $is_active,
                trainer_id = $trainer_id,
                locker_id = $locker_id
                WHERE id = $id;
            ", connection);
        insertcommand.Parameters.AddWithValue("$surname", client.surname);
        insertcommand.Parameters.AddWithValue("$name", client.name);
        insertcommand.Parameters.AddWithValue("$patronymic", client.patronymic is null ? (object)DBNull.Value : client.patronymic);
        insertcommand.Parameters.AddWithValue("$birthday", client.birthday);
        insertcommand.Parameters.AddWithValue("$phone", client.phone);
        insertcommand.Parameters.AddWithValue("$email", client.email);
        insertcommand.Parameters.AddWithValue("$is_active", client.is_active);
        insertcommand.Parameters.AddWithValue("$trainer_id", client.trainer_id.HasValue ? client.trainer_id : (object)DBNull.Value);
        insertcommand.Parameters.AddWithValue("$locker_id", client.locker_id.HasValue ? client.locker_id : (object)DBNull.Value);
        insertcommand.Parameters.AddWithValue("$id", id);
        insertcommand.ExecuteNonQuery();
    }
    client.id = Guid.Parse(id);
    return Results.Json(client);
});
app.MapGet("/api/clients", () =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients;", connection);
    using var reader = command.ExecuteReader();
    var result = new List<object>();
    while (reader.Read())
    {
        result.Add(new
        {
            id = reader.GetString(0),
            surname = reader.GetString(1),
            name = reader.GetString(2),
            patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
            birthday = reader.GetString(4),
            phone = reader.GetString(5),
            email = reader.GetString(6),
            is_active = reader.GetInt32(7) == 1,
            trainer_id = reader.IsDBNull(8) ? null : reader.GetString(8),
            locker_id = reader.IsDBNull(9) ? null : reader.GetString(9)
        });
    }
    return Results.Json(result);
});
app.MapGet("/api/clients/{id}", (string id) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE id = $id;", connection);
    command.Parameters.AddWithValue("$id", id.ToUpper());

    using var reader = command.ExecuteReader();
    if (!reader.Read()) return Results.NotFound();

    var result = new
    {
        id = reader.GetString(0),
        surname = reader.GetString(1),
        name = reader.GetString(2),
        patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
        birthday = reader.GetString(4),
        phone = reader.GetString(5),
        email = reader.GetString(6),
        is_active = reader.GetInt32(7) == 1,
        trainer_id = reader.IsDBNull(8) ? null : reader.GetString(8),
        locker_id = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
    return Results.Json(result);
});
app.MapGet("/api/clients/{id}/detail", (string id) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var clientCommand = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE id = $id;", connection);
    clientCommand.Parameters.AddWithValue("$id", id.ToUpper());

    using var clientReader = clientCommand.ExecuteReader();
    if (!clientReader.Read()) return Results.NotFound();

    var client = new
    {
        id = clientReader.GetString(0),
        surname = clientReader.GetString(1),
        name = clientReader.GetString(2),
        patronymic = clientReader.IsDBNull(3) ? null : clientReader.GetString(3),
        birthday = clientReader.GetString(4),
        phone = clientReader.GetString(5),
        email = clientReader.GetString(6),
        is_active = clientReader.GetInt32(7) == 1
    };

    object trainer = null;
    if (!clientReader.IsDBNull(8) && clientReader.GetString(8) is string trainerId)
    {
        using var trainerCommand = new SqliteCommand(@"
            SELECT id, surname, name, patronymic, phone, status
            FROM trainers WHERE id = $id", connection);
        trainerCommand.Parameters.AddWithValue("$id", trainerId);
        using var trainerReader = trainerCommand.ExecuteReader();
        if (trainerReader.Read()) trainer = new
        {
            id = trainerReader.GetString(0),
            surname = trainerReader.GetString(1),
            name = trainerReader.GetString(2),
            patronymic = trainerReader.IsDBNull(3) ? null : trainerReader.GetString(3),
            phone = trainerReader.GetString(4),
            status = ((Status)trainerReader.GetInt32(5)).ToString()
        };
    }

    object locker = null;
    if (!clientReader.IsDBNull(9) && clientReader.GetString(9) is string lockerId) {
        using var lockerCommand = new SqliteCommand(@"
            SELECT id, number
            FROM lockers WHERE id = $id", connection);
        lockerCommand.Parameters.AddWithValue("$id", lockerId);
        using var lockerReader = lockerCommand.ExecuteReader();
        if (lockerReader.Read()) locker = new {
            id = lockerReader.GetString(0),
            number = lockerReader.GetInt32(1)
        };
    }

    var services = new List<object>();
    using var servicesCommand = new SqliteCommand(@"
        SELECT id, name, price
        FROM services s
        JOIN client_services cs ON cs.service_id = s.id
        WHERE cs.client_id = $id", connection);
    servicesCommand.Parameters.AddWithValue("$id", client.id);
    using var servicesReader = servicesCommand.ExecuteReader();
    while (servicesReader.Read())
    {
        services.Add(new
        {
            id = servicesReader.GetString(0),
            name = servicesReader.GetString(1),
            price = servicesReader.GetInt32(2),
        });
    }

    var result = new
    {
        client.id,
        client.surname,
        client.name,
        client.patronymic,
        client.birthday,
        client.phone,
        client.email,
        client.is_active,
        trainer,
        locker,
        services
    };

    return Results.Json(result);
});
app.MapPatch("/api/clients/{id}/status", (string id, Client client) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var existsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM clients WHERE id = $id);", connection);
    existsCommand.Parameters.AddWithValue("$id", id.ToUpper());
    var exists = (long)existsCommand.ExecuteScalar();
    if (exists != 1) return Results.NotFound();
    else
    {
        var insertcommand = new SqliteCommand(@"
            UPDATE clients
            SET is_active = $is_active
            WHERE id = $id;
            ", connection);
        insertcommand.Parameters.AddWithValue("$is_active", client.is_active);
        insertcommand.Parameters.AddWithValue("$id", id.ToUpper());
        insertcommand.ExecuteNonQuery();
    }

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE id = $id;", connection);
    command.Parameters.AddWithValue("$id", id.ToUpper());

    using var reader = command.ExecuteReader();
    if (!reader.Read()) return Results.NotFound();

    var result = new
    {
        id = reader.GetString(0),
        surname = reader.GetString(1),
        name = reader.GetString(2),
        patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
        birthday = reader.GetString(4),
        phone = reader.GetString(5),
        email = reader.GetString(6),
        is_active = reader.GetInt32(7) == 1,
        trainer_id = reader.IsDBNull(8) ? null : reader.GetString(8),
        locker_id = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
    return Results.Json(result);
});
app.MapPost("/api/clients/{clientId}/trainer/{trainerId}", (string clientId, string trainerId) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var clientExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM clients WHERE id = $id);", connection);
    clientExistsCommand.Parameters.AddWithValue("$id", clientId.ToUpper());
    var clientExists = (long)clientExistsCommand.ExecuteScalar();
    if (clientExists != 1) return Results.NotFound();
    else
    {
        var trainerExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM trainers WHERE id = $id);", connection);
        trainerExistsCommand.Parameters.AddWithValue("$id", trainerId.ToUpper());
        var trainerExists = (long)trainerExistsCommand.ExecuteScalar();
        if (trainerExists != 1) return Results.NotFound();
        else
        {
            var insertcommand = new SqliteCommand(@"
                UPDATE clients
                SET trainer_id = $trainerId
                WHERE id = $clientId;
                ", connection);
            insertcommand.Parameters.AddWithValue("$trainerId", trainerId.ToUpper());
            insertcommand.Parameters.AddWithValue("$clientId", clientId.ToUpper());
            insertcommand.ExecuteNonQuery();
        }
    }

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE id = $id;", connection);
    command.Parameters.AddWithValue("$id", clientId.ToUpper());

    using var reader = command.ExecuteReader();
    if (!reader.Read()) return Results.NotFound();

    var result = new
    {
        id = reader.GetString(0),
        surname = reader.GetString(1),
        name = reader.GetString(2),
        patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
        birthday = reader.GetString(4),
        phone = reader.GetString(5),
        email = reader.GetString(6),
        is_active = reader.GetInt32(7) == 1,
        trainer_id = reader.IsDBNull(8) ? null : reader.GetString(8),
        locker_id = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
    return Results.Json(result);
});
app.MapPost("/api/clients/{clientId}/locker/{lockerId}", (string clientId, string lockerId) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var clientExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM clients WHERE id = $id);", connection);
    clientExistsCommand.Parameters.AddWithValue("$id", clientId.ToUpper());
    var clientExists = (long)clientExistsCommand.ExecuteScalar();
    if (clientExists != 1) return Results.NotFound();
    else
    {
        var lockerExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM lockers WHERE id = $id);", connection);
        lockerExistsCommand.Parameters.AddWithValue("$id", lockerId.ToUpper());
        var lockerExists = (long)lockerExistsCommand.ExecuteScalar();
        if (lockerExists != 1) return Results.NotFound();
        else
        {
            var lockerEmptyCommand = new SqliteCommand("SELECT client_id FROM lockers WHERE id = $id;", connection);
            lockerEmptyCommand.Parameters.AddWithValue("$id", lockerId.ToUpper());
            var lockerEmpty = lockerEmptyCommand.ExecuteScalar();
            if (lockerEmpty != null && lockerEmpty != DBNull.Value) return Results.Conflict();
            else
            {
                var insertcommand = new SqliteCommand(@"
                    UPDATE clients
                    SET locker_id = $lockerId
                    WHERE id = $clientId;
                    UPDATE lockers
                    SET client_id = $clientId
                    WHERE id = $lockerId;
                    ", connection);
                insertcommand.Parameters.AddWithValue("$lockerId", lockerId.ToUpper());
                insertcommand.Parameters.AddWithValue("$clientId", clientId.ToUpper());
                insertcommand.ExecuteNonQuery();
            }
        }
    }

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE id = $id;", connection);
    command.Parameters.AddWithValue("$id", clientId.ToUpper());

    using var reader = command.ExecuteReader();
    if (!reader.Read()) return Results.NotFound();

    var result = new
    {
        id = reader.GetString(0),
        surname = reader.GetString(1),
        name = reader.GetString(2),
        patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
        birthday = reader.GetString(4),
        phone = reader.GetString(5),
        email = reader.GetString(6),
        is_active = reader.GetInt32(7) == 1,
        trainer_id = reader.IsDBNull(8) ? null : reader.GetString(8),
        locker_id = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
    return Results.Json(result);
});
app.MapPost("/api/clients/{clientId}/additionalServices/{serviceId}", (string clientId, string serviceId) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var clientExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM clients WHERE id = $id);", connection);
    clientExistsCommand.Parameters.AddWithValue("$id", clientId.ToUpper());
    var clientExists = (long)clientExistsCommand.ExecuteScalar();
    if (clientExists != 1) return Results.NotFound();
    else
    {
        var serviceExistsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM services WHERE id = $id);", connection);
        serviceExistsCommand.Parameters.AddWithValue("$id", serviceId.ToUpper());
        var serviceExists = (long)serviceExistsCommand.ExecuteScalar();
        if (serviceExists != 1) return Results.NotFound();
        else
        {
            var insertcommand = new SqliteCommand(@"
                INSERT INTO client_services (client_id, service_id)
                VALUES ($clientId, $serviceId)", connection);
            insertcommand.Parameters.AddWithValue("$clientId", clientId.ToUpper());
            insertcommand.Parameters.AddWithValue("$serviceId", serviceId.ToUpper());
            insertcommand.ExecuteNonQuery();
        }
    }

    var pair = new { client_id = clientId, service_id = serviceId };
    return Results.Json(pair);
});

app.MapPost("/api/trainers", (Trainer trainer) => {
    trainer.id = Guid.NewGuid();
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();

        using var command = new SqliteCommand(@"
            INSERT INTO trainers (id, surname, name, patronymic, phone, status) VALUES
            ($id, $surname, $name, $patronymic, $phone, $status);
        ", connection);
        command.Parameters.AddWithValue("$id", trainer.id);
        command.Parameters.AddWithValue("$surname", trainer.surname);
        command.Parameters.AddWithValue("$name", trainer.name);
        command.Parameters.AddWithValue("$patronymic", trainer.patronymic ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$phone", trainer.phone);
        command.Parameters.AddWithValue("$status", (int)trainer.status);
        command.ExecuteNonQuery();
    }
    return trainer;
});
app.MapPut("/api/trainers/{id}", (string id, Trainer trainer) => {
    using (var connection = new SqliteConnection(connectionString))
    {
        trainer.id = Guid.Parse(id);
        connection.Open();

        var existsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM trainers WHERE id = $id);", connection);
        existsCommand.Parameters.AddWithValue("$id", id.ToUpper());
        var exists = (long)existsCommand.ExecuteScalar();
        if (exists != 1) return Results.NotFound();
        using var command = new SqliteCommand(@"
            UPDATE trainers
            SET surname = $surname,
            name = $surname,
            patronymic = $patronymic,
            phone = $phone,
            status = $status
            WHERE id = $id;
        ", connection);
        command.Parameters.AddWithValue("$id", id.ToUpper());
        command.Parameters.AddWithValue("$surname", trainer.surname);
        command.Parameters.AddWithValue("$name", trainer.name);
        command.Parameters.AddWithValue("$patronymic", trainer.patronymic ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$phone", trainer.phone);
        command.Parameters.AddWithValue("$status", (int)trainer.status);
        command.ExecuteNonQuery();
    }
    return Results.Ok(trainer);
});
app.MapPatch("/api/trainers/{id}/status", (string id, Trainer trainer) => {
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();

        var existsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM trainers WHERE id = $id);", connection);
        existsCommand.Parameters.AddWithValue("$id", id);
        var exists = (long)existsCommand.ExecuteScalar();
        if (exists != 1) return Results.NotFound();
        using var command = new SqliteCommand(@"
            UPDATE trainers
            SET status = $status
            WHERE id = $id;
        ", connection);
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$status", (int)trainer.status);
        command.ExecuteNonQuery();

        using var resultCommand = new SqliteCommand(@"
            SELECT id, surname, name, patronymic, phone, status
            FROM trainers WHERE id = $id;", connection);
        resultCommand.Parameters.AddWithValue("$id", id.ToUpper());

        using var resultReader = resultCommand.ExecuteReader();
        if (!resultReader.Read()) return Results.NotFound();

        var result = new
        {
            id = resultReader.GetString(0),
            surname = resultReader.GetString(1),
            name = resultReader.GetString(2),
            patronymic = resultReader.IsDBNull(3) ? null : resultReader.GetString(3),
            phone = resultReader.GetString(4),
            status = ((Status)resultReader.GetInt32(5)).ToString()
        };

        return Results.Json(result);
    }
});
app.MapGet("/api/trainers/{id}/detail", (string id) => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var trainerCommand = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, phone, status
        FROM trainers WHERE id = $id;", connection);
    trainerCommand.Parameters.AddWithValue("$id", id.ToUpper());

    using var trainerReader = trainerCommand.ExecuteReader();
    if (!trainerReader.Read()) return Results.NotFound();

    var trainer = new
    {
        id = trainerReader.GetString(0),
        surname = trainerReader.GetString(1),
        name = trainerReader.GetString(2),
        patronymic = trainerReader.IsDBNull(3) ? null : trainerReader.GetString(3),
        phone = trainerReader.GetString(4),
        status = ((Status)trainerReader.GetInt32(5)).ToString()
    };

    var clients = new List<object>();
    using var clientsCommand = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, birthday, phone, email, is_active, trainer_id, locker_id
        FROM clients WHERE trainer_id = $id;", connection);
    clientsCommand.Parameters.AddWithValue("$id", id.ToUpper());
    using var clientsReader = clientsCommand.ExecuteReader();
    while (clientsReader.Read())
    {
        clients.Add(new
        {
            id = clientsReader.GetString(0),
            surname = clientsReader.GetString(1),
            name = clientsReader.GetString(2),
            patronymic = clientsReader.IsDBNull(3) ? null : clientsReader.GetString(3),
            birthday = clientsReader.GetString(4),
            phone = clientsReader.GetString(5),
            email = clientsReader.GetString(6),
            is_active = clientsReader.GetInt32(7) == 1,
            trainer_id = clientsReader.IsDBNull(8) ? null : clientsReader.GetString(8),
            locker_id = clientsReader.IsDBNull(9) ? null : clientsReader.GetString(9)
        });
    }

    var result = new
    {
        trainer.id,
        trainer.surname,
        trainer.name,
        trainer.patronymic,
        trainer.status,
        clients
    };

    return Results.Json(result);
});
app.MapGet("/api/trainers", () => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand(@"
        SELECT id, surname, name, patronymic, phone, status
        FROM trainers;", connection);
    using var reader = command.ExecuteReader();
    var result = new List<object>();
    while (reader.Read())
    {
        result.Add(new
        {
            id = reader.GetString(0),
            surname = reader.GetString(1),
            name = reader.GetString(2),
            patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
            phone = reader.GetString(4),
            status = ((Status)reader.GetInt32(5)).ToString()
        });
    }
    return Results.Json(result);
});

app.MapGet("/api/lockers", () => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand(@"
        SELECT json_group_array(json_object(
            'id', id,
            'number', number,
            'client_id', client_id
        )) FROM lockers;
        ", connection);
    var jsonResult = command.ExecuteScalar()?.ToString() ?? "[]";
    return Results.Content(jsonResult, "application/json");
});

app.MapGet("/api/additionalServices", () => {
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var command = new SqliteCommand(@"
        SELECT json_group_array(json_object(
            'id', id,
            'name', name,
            'price', price
        )) FROM services;
        ", connection);
    var jsonResult = command.ExecuteScalar()?.ToString() ?? "[]";
    return Results.Content(jsonResult, "application/json");
});
app.MapGet("/api/additionalServices/{id}", (string id) => {
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();

        var existsCommand = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM services WHERE id = $id);", connection);
        existsCommand.Parameters.AddWithValue("$id", id);
        var exists = (long)existsCommand.ExecuteScalar();
        if (exists != 1) return Results.NotFound();
        var command = new SqliteCommand(@"
            SELECT json_object(
                'id', id,
                'name', name,
                'price', price
            ) FROM services WHERE id = $id;
            ", connection);
        command.Parameters.AddWithValue("$id", id);
        var jsonResult = command.ExecuteScalar()?.ToString() ?? "{}";
        return Results.Content(jsonResult, "application/json");
    }
});

app.Run();

class Client
{
    public Guid id { get; set; }
    public string surname { get; set; }
    public string name { get; set; }
    public string patronymic { get; set; }
    public DateOnly birthday { get; set; }
    public string phone { get; set; }
    public string email { get; set; }
    public bool is_active { get; set; } = true;
    public Guid? trainer_id { get; set; }
    public Guid? locker_id { get; set; }
}

class Trainer {
    public Guid id { get; set; }
    public string surname { get; set; }
    public string name { get; set; }
    public string? patronymic { get; set; }
    public string phone { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status status { get; set; }
}

class Locker {
    public Guid id { get; set; }
    public int number { get; set; }
    public Guid? client_id { get; set; }
}

class Service {
    public string id { get; set; }
    public string name { get; set; }
    public int price { get; set; }
}

class Client_Service {
    public Guid client_id { get; set; }
    public string service_id { get; set; }
}

enum Status {
    WORKING,
    ON_LEAVE,
    NOT_WORKING
}
