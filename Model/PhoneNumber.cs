﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace BillingSystem.Model
{
    public class PhoneNumber
    {
        private long _id;

        public long ID
        {
            get { return _id; }
        }

        private long _subscriberID;

        public long SubscriberID
        {
            get { return _subscriberID; }
            set { _subscriberID = value; }
        }

        private string _number;

        public string Number
        {
            get { return _number; }
            set { /*_number = value;*/ }
        }

        private long _tariffID;

        private long _newTariffID = -1;

        public long TariffID
        {
            get
            {
                return _newTariffID > 0 ? _newTariffID : _tariffID;
            }
            set { _newTariffID = value; }
        }

        private static MySqlConnection _connection;

        private static MySqlConnection connection
        {
            get
            {
                if (_connection == null)
                    _connection = ConnectionManager.GetConnection();
                return _connection;
            }
        }

        public PhoneNumber(long id, long subscriberID, string number, long tariffID)
        {
            _id = id;
            _subscriberID = subscriberID;
            _number = number;
            _tariffID = tariffID;
        }

        /// <summary>
        /// Получает телефонный номер из БД по его id.
        /// </summary>
        /// <returns>Номер</returns>
        public static PhoneNumber SelectPhoneNumberByID(long phoneNumberID)
        {
            PhoneNumber result = null;
            try
            {
                connection.Open();

                MySqlCommand cmd = new MySqlCommand("SELECT * FROM phone_number WHERE id = " + phoneNumberID.ToString(), connection);
                MySqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    result = new PhoneNumber(r.GetInt64("id"), r.GetInt64("subscriber_id"), r.GetString("number"), r.GetInt64("tariff_id"));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Получает телефонный номер из БД по значению номера.
        /// </summary>
        /// <param name="phoneNumber">Значение номера</param>
        /// <returns></returns>
        public static PhoneNumber SelectPhoneNumberByNumber(string phoneNumber)
        {
            PhoneNumber result = null;
            try
            {
                connection.Open();

                MySqlCommand cmd = new MySqlCommand("SELECT * FROM phone_number WHERE number = '" + phoneNumber + "'", connection);
                MySqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    result = new PhoneNumber(r.GetInt64("id"), r.GetInt64("subscriber_id"), r.GetString("number"), r.GetInt64("tariff_id"));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Обновляет телефон в БД.
        /// </summary>
        public void Update()
        {
            try
            {
                connection.Open();
                string queryString = "UPDATE phone_number SET number = '" + Number + "', subscriber_id = " + SubscriberID.ToString() + ", tariff_id = " + TariffID.ToString() + " WHERE id = " + ID.ToString() + " OR number = '" + Number + "'";
                MySqlCommand cmd = new MySqlCommand(queryString, connection);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            if (_tariffID != _newTariffID)
            {
                updateTariffHistory();
                if (_tariffID > 0)
                {
                    Service s = Service.SelectChangeTariffService();
                    Charge ch = new Charge(this, s, 0);
                    ch.WriteOff();
                }
            }
            _tariffID = _newTariffID;
            _newTariffID = -1;
        }

        private void updateTariffHistory()
        {
            try
            {
                connection.Open();
                DateTime date = DateTime./*Utc*/Now;
                string queryString = @"UPDATE tariff_history SET end_date = @end WHERE phone_id = @phone_id AND tariff_id = @tariff_id AND end_date IS NULL;
                                     INSERT INTO tariff_history (phone_id, tariff_id, start_date) VALUES (@phone_id, @new_tariff_id, @end)";
                MySqlCommand cmd = new MySqlCommand(queryString, connection);
                cmd.Parameters.AddWithValue("@end", DateTime./*Utc*/Now);
                cmd.Parameters.AddWithValue("@phone_id", ID);
                cmd.Parameters.AddWithValue("@tariff_id", _tariffID);
                cmd.Parameters.AddWithValue("@new_tariff_id", _newTariffID);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Добавляет телефон в БД.
        /// </summary>
        public void Add()
        {
            try
            {
                connection.Open();
                string queryString = @"INSERT INTO phone_number (number,  subscriber_id, tariff_id) VALUES  (@number, @subscriber_id, @new_tariff_id);
                                     INSERT INTO tariff_history (phone_id, tariff_id, start_date) VALUES ((SELECT MAX(id) FROM phone_number WHERE number LIKE(@number)), @new_tariff_id, @start)";
                MySqlCommand cmd = new MySqlCommand(queryString, connection);
                cmd.Parameters.AddWithValue("@subscriber_id", _subscriberID);
                cmd.Parameters.AddWithValue("@number", _number);
                cmd.Parameters.AddWithValue("@new_tariff_id", _newTariffID);
                cmd.Parameters.AddWithValue("@start", DateTime./*Utc*/Now);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Удаляет телефон из БД.
        /// </summary>
        public void Delete()
        {
            try
            {
                connection.Open();
                string queryString = "DELETE FROM phone_number WHERE id = " + ID.ToString() + " OR number = '" + Number + "'";
                MySqlCommand cmd = new MySqlCommand(queryString, connection);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Возвращает новый не занятый номер телефона.
        /// </summary>
        /// <returns>Телефон</returns>
        public static PhoneNumber GetNewNumber()
        {
            PhoneNumber result = null;
            long number;
            try
            {
                connection.Open();

                MySqlCommand cmd = new MySqlCommand("SELECT MAX(number) FROM free_phone_number", connection);
                MySqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    number = r.GetInt64("MAX(number)");
                    r.Close();
                    MySqlCommand cmd2 = new MySqlCommand("UPDATE free_phone_number SET number = @number", connection);
                    cmd2.Parameters.AddWithValue("@number", number + 1);
                    cmd2.ExecuteNonQuery();
                    result = new PhoneNumber(-1, -1, "+" + number.ToString(), -1);
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Возвращает тариф для этого телефона в указанный момент времени
        /// </summary>
        /// <param name="date">Дата и время</param>
        /// <returns>Тариф</returns>
        public Tariff SelectTariffByDate(DateTime date)
        {
            Tariff result = null;
            try
            {
                connection.Open();

                string query = "SELECT * FROM tariff WHERE id = (SELECT tariff_id FROM tariff_history WHERE phone_id = @phone_id AND start_date <= @date AND (end_date > @date OR end_date IS NULL))";

                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@phone_id", ID);
                cmd.Parameters.AddWithValue("@date", date/*.ToUniversalTime()*/);

                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    result = new Tariff(r.GetInt64("id"), r.GetString("name"), DatabaseUtils.GetStringOrNull(r, "description"), r.GetBoolean("active"));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());

            }
            finally
            {
                connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Возвращает все звонки для данного номера за указанный период
        /// </summary>
        /// <param name="from">Дата начала периода поиска</param>
        /// <param name="to">Дата конца периода поиска</param>
        /// <returns>Звонки</returns>
        public List<Call> SelectCalls(DateTime from, DateTime to)
        {
            List<Call> result = new List<Call>();
            try
            {
                connection.Open();

                string query = "SELECT * FROM calls WHERE (calling_number = '" + Number + "' OR called_number = '" + Number + "')"
                    + " AND DATE(start_time) >= DATE(@from) AND DATE(start_time) <= DATE(@to) AND end_time IS NOT NULL ORDER BY start_time, end_time, called_number, calling_number";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@from", from/*.ToUniversalTime()*/);
                cmd.Parameters.AddWithValue("@to", to/*.ToUniversalTime()*/);
                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    result.Add(new Call(r.GetInt64("id"), r.GetString("calling_number"),
                        r.GetString("called_number"), r.GetDateTime("start_time"), r.GetDateTime("end_time")));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Возвращает историю смены тарифов
        /// </summary>
        /// <param name="from">Дата начала периода поиска</param>
        /// <param name="to">Дата конца периода поиска</param>
        /// <returns>История тарифов</returns>
        public List<TariffHistory> SelectTariffHistory(DateTime from, DateTime to)
        {
            List<TariffHistory> result = new List<TariffHistory>();
            try
            {
                connection.Open();
                string query = @"SELECT th.id id, th.phone_id phone_id, th.tariff_id tariff_id,
                    t.name name, th.start_date start_date, 
                    IF(th.end_date IS NULL, NULL, DATE(th.end_date)) end_date 
                    FROM tariff_history th, tariff t, service s
                    WHERE th.phone_id = @phone_id
                    AND t.id = th.tariff_id
                    AND LOWER(s.name) LIKE LOWER(@param)
                    AND (DATE(th.start_date) >= DATE(@from) AND DATE(th.start_date) <= DATE(@to))
                    AND (DATE(th.end_date) <= DATE(@to) OR th.end_date IS NULL)
                    AND (DATE(th.end_date) >= DATE(@from) OR th.end_date IS NULL)";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@phone_id", ID);
                cmd.Parameters.AddWithValue("@from", from/*.ToUniversalTime()*/);
                cmd.Parameters.AddWithValue("@to", to/*.ToUniversalTime()*/);
                cmd.Parameters.AddWithValue("@param", Constants.TariffChanging);
                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    result.Add(new TariffHistory(r.GetInt64("id"),
                        r.GetInt64("phone_id"),
                        r.GetInt64("tariff_id"),
                        r.GetString("name"),
                        r.GetDateTime("start_date"),
                        r.IsDBNull(r.GetOrdinal("end_date")) ? null : (DateTime?) r.GetDateTime("end_date")));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Возвращает идентификатор абонента по имеющемуся у него телефонному номеру
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <returns>Идентификатор абонента</returns>
        public Int64 GetSubscriberIDByPhoneNumber(string phoneNumber)
        {
            Int64 result = new Int64();
            try
            {
                connection.Open();
                string query = @"SELECT subscriber_id FROM phone_number WHERE number = @number";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@number", phoneNumber);
                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    result = r.GetInt64("subscriber_id");
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Возвращает платежи указанного абонента за указанный период
        /// </summary>
        /// <param name="subscriberID">Идентификатор абонента</param>
        /// <param name="from">Начальная дата указанног периода</param>
        /// <param name="to">Конечная дата указанного периода</param>
        /// <returns>Спсисок платежей</returns>
        public List<Payment> SearchPaymentsForSubscriber(long subscriberID, DateTime from, DateTime to)
        {
            List<Payment> searchResult = new List<Payment>();
            try
            {
                connection.Open();
                string query = @"SELECT id, subscriber_id, sum, date FROM payment WHERE subscriber_id = @subscriber_id
                    AND (DATE(date) <= DATE(@to) AND DATE(date) >= DATE(@from))";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@subscriber_id", subscriberID);
                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    searchResult.Add(new Payment(r.GetInt64("id"), r.GetInt64("subscriber_id"),
                        r.GetDouble("sum"), r.GetDateTime("date")));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return searchResult;
        }

        /// <summary>
        /// Возвращает список использованных услуг у абонента за указанный период
        /// </summary>
        /// <param name="phoneNumber">Номер телефона абонента</param>
        /// <param name="from">Начальная дата указанного периода</param>
        /// <param name="to">Конечная дата указанного периода</param>
        /// <returns></returns>
        public List<Charge> SearchCharges(PhoneNumber phoneNumber, DateTime from, DateTime to)
        {
            List<Charge> searchResult = new List<Charge>();
            try
            {
                connection.Open();
                string query = @"SELECT id, phone_id, service_id, sum, date FROM charge
                    WHERE phone_id = @phone_id AND (DATE(date) >= DATE(@from) AND DATE(date) <= DATE(@to))";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@phone_id", phoneNumber.ID);
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    searchResult.Add(new Charge(r.GetInt64("id"), r.GetInt64("phone_id"), r.GetInt64("service_id"),
                        r.GetDouble("sum"), r.GetDateTime("date")));
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return searchResult;
        }


        public static Int64 SelectPhoneNumberIDByNumber(string phoneNumber)
        {
            Int64 result = new Int64();
            try
            {
                connection.Open();
                string query = @"SELECT id from phone_number WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", phoneNumber);
                MySqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    result = r.GetInt64("id");
                }
                r.Close();
            }
            catch (MySqlException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                connection.Close();
            }
            return result;
        }
        
        /// <summary>
        /// Возвращает текущий тариф
        /// </summary>
        /// <returns>Текущий тариф</returns>
        public Tariff GetTariff()
        {
            return Tariff.SelectTariffByID(TariffID);
        }
    }
}
