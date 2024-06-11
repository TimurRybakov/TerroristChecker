from sqlalchemy import create_engine
import pandas as pd
import aiohttp
import asyncio
from aiohttp import ClientSession
from urllib.parse import urlencode
from datetime import datetime
from aiohttp import ClientTimeout
import urllib3

urllib3.disable_warnings()

engine = create_engine('mssql+pyodbc://ng-pdsqlngt2.hq.qiwi.com/Works?trusted_connection=yes&driver=ODBC+Driver+17+for+SQL+Server')


def get_test_data():
    sql_query_all = """select 
    T.id, 
    SUBSTRING(T.client_name, 1, CHARINDEX(' ', T.client_name + ' ') - 1) AS LastName,
    SUBSTRING(T.client_name, CHARINDEX(' ', T.client_name + ' ') + 1, CHARINDEX(' ', T.client_name + ' ', CHARINDEX(' ', T.client_name + ' ') + 1) - CHARINDEX(' ', T.client_name + ' ') - 1) AS FirstName,
    SUBSTRING(T.client_name, CHARINDEX(' ', T.client_name + ' ', CHARINDEX(' ', T.client_name + ' ') + 1) + 1, LEN(T.client_name)) AS Patronymic, 
    case
        when T.birthday IS NULL OR T.birthday = '' then null
        else CONVERT(date, T.birthday, 112)
    end as Birthday, 
    T.result 
from
(select id, REPLACE(REPLACE(REPLACE(client_name,' ','<>'),'><',''),'<>',' ') as client_name, birthday, result from Contact..TCNT_KFM_CHECK with(nolock)
) as T"""
    sql_query_light = """select 
    T.id, 
    SUBSTRING(T.client_name, 1, CHARINDEX(' ', T.client_name + ' ') - 1) AS LastName,
    SUBSTRING(T.client_name, CHARINDEX(' ', T.client_name + ' ') + 1, CHARINDEX(' ', T.client_name + ' ', CHARINDEX(' ', T.client_name + ' ') + 1) - CHARINDEX(' ', T.client_name + ' ') - 1) AS FirstName,
    SUBSTRING(T.client_name, CHARINDEX(' ', T.client_name + ' ', CHARINDEX(' ', T.client_name + ' ') + 1) + 1, LEN(T.client_name)) AS Patronymic, 
    case
        when T.birthday IS NULL OR T.birthday = '' then null
        else CONVERT(date, T.birthday, 112)
    end as Birthday, 
    T.result 
from
(select id, REPLACE(REPLACE(REPLACE(client_name,' ','<>'),'><',''),'<>',' ') as client_name, birthday, result from Contact..TCNT_KFM_CHECK with(nolock) where result > 0
) as T"""
    return pd.read_sql_query(sql_query_all, engine)


async def send_request(session, params):
    try:
        query_string = urlencode(params)
        url = f"http://localhost:7081/terrorists/search?{query_string}"
        # Установка таймаута для запроса
        timeout = ClientTimeout(total=60)  # Увеличить таймаут до 60 секунд
        session.Verify = False
        async with session.get(url, timeout=timeout) as response:
            response.raise_for_status()
            data = await response.json()
            return data
    except aiohttp.ClientError as e:
        return {'error': str(e)}
    except asyncio.TimeoutError:
        return {'error': 'Timeout occurred'}


async def start_test():
    print("Начинаем тест")
    test_data = get_test_data()
    total_requests = len(test_data)
    print(f"Данные загружены. Всего: {total_requests} записей")

    matches = 0
    no_matches_found = 0
    no_matches_response = 0
    error_requests = []
    discrepancies = []

    # Ограничение параллельности запросов
    semaphore = asyncio.Semaphore(50)  # Максимум 50 параллельных запросов

    async def bounded_send_request(row):
        async with semaphore:
            params = {
                'input': row['FirstName'] + ' ' + row['LastName'] + ' ' + row['Patronymic']
            }
            if row['Birthday'] is not None:
                params['birthday'] = row['Birthday']
            return await send_request(session, params)

    async with aiohttp.ClientSession() as session:
        tasks = [bounded_send_request(row) for index, row in test_data.iterrows()]

        responses = await asyncio.gather(*tasks)
        for i, result in enumerate(responses):
            row = test_data.iloc[i]
            if 'error' in result:
                error_requests.append((i, result['error']))
            else:
                if len(result) != 0:
                    record_id = result[0]['id']
                    # record_id = result.get("terroristId", 0)
                    if row['result'] != 0:
                        matches += 1
                    else:
                        discrepancies.append((row['id'], row['LastName'], row['FirstName'], row['Patronymic'], row['Birthday'], row['result'], record_id))
                        no_matches_found += 1 if row['result'] != 0 else 0
                        no_matches_response += 1 if row['result'] == 0 else 0
                else:
                    if row['result'] != 0:
                        discrepancies.append((row['id'], row['LastName'], row['FirstName'], row['Patronymic'], row['Birthday'], row['result'], 0))
                        no_matches_response += 1
                    else:
                        matches += 1

    print("\nСтатистика выполнения запросов:")
    print(f"Количество совпавших результатов: {matches}")
    print(f"Количество несовпавших результатов (найдено, но не совпадает ID): {no_matches_found}")
    print(f"Количество несовпавших результатов (ожидалось найти, но ответ пуст): {no_matches_response}")
    print(f"\nВсего неудачных запросов: {len(error_requests)}")
    for err in error_requests:
        print(f"Запрос {err[0]}: Ошибка - {err[1]}")
    if discrepancies:
        print("\nПодробная информация о несовпадениях:")
        for disc in discrepancies:
            print(f"ID: {disc[0]}, ФИО: {disc[1]} {disc[2]} {disc[3]}, Дата рождения: {disc[4]}, Результат в таблице: {disc[5]}, Результат запроса: {disc[6]}")


if __name__ == '__main__':
    start_time = datetime.now()
    asyncio.run(start_test())
    end_time = datetime.now()
    elapsed_time = end_time - start_time
    print("Тестирование завершено за", elapsed_time)

