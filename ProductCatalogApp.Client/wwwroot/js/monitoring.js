(() => {
    const element = document.getElementById('monitoring');
    const byId = id => document.getElementById(id);
    const connection = new signalR.HubConnectionBuilder().withUrl(element.dataset.hubUrl, { accessTokenFactory: () => element.dataset.accessToken })
        .withAutomaticReconnect([0, 2000, 5000, 10000]).build();
    let readings = [], sensors = [], retry;
    const showError = error => { byId('error').textContent = error.message || String(error); byId('error').hidden = false; };
    function state(text, connected) {
        byId('status').textContent = text;
        byId('status').className = 'badge ' + (connected ? 'bg-success' : 'bg-secondary');
        byId('submit').disabled = !connected;
    }
    async function join() { await connection.invoke('JoinSite', byId('site').value ? Number(byId('site').value) : null); }
    function render() {
        byId('readings').replaceChildren(...readings.map(reading => {
            const tr = document.createElement('tr');
            if (reading.isAlarm) tr.className = 'table-warning';
            for (const value of [new Date(reading.timestamp).toLocaleString(), reading.sensorName,
                reading.value + ' ' + reading.unit, reading.source, reading.isAlarm ? 'Тревога' : 'Норма']) {
                const td = document.createElement('td'); td.textContent = value; tr.append(td);
            }
            return tr;
        }));
    }
    connection.on('OnlineChanged', count => byId('online').textContent = count);
    connection.on('ReceiveSnapshot', snapshot => {
        sensors = snapshot.sensors; readings = snapshot.readings; render();
        byId('sensor').replaceChildren(...sensors.map(sensor => {
            const option = document.createElement('option'); option.value = sensor.id;
            option.textContent = sensor.name + ' (' + sensor.minValue + '–' + sensor.maxValue + ' ' + sensor.unit + ')'; return option;
        }));
    });
    connection.on('ReadingReceived', reading => { readings = [reading, ...readings.filter(r => r.id !== reading.id)].slice(0, 50); render(); });
    connection.on('ReadingAccepted', id => { byId('accepted').textContent = 'Замер №' + id + ' сохранён.'; byId('error').hidden = true; });
    connection.on('AlarmRaised', reading => { byId('alarm').textContent = 'Тревога: ' + reading.sensorName + ', ' + reading.value + ' ' + reading.unit; byId('alarm').hidden = false; });
    connection.onreconnecting(() => state('Переподключение…', false));
    connection.onreconnected(async () => { state('Подключено', true); try { await join(); } catch (e) { showError(e); } });
    connection.onclose(() => { state('Нет связи', false); retry = setTimeout(start, 5000); });
    byId('site').addEventListener('change', () => join().catch(showError));
    byId('reading-form').addEventListener('submit', async event => {
        event.preventDefault();
        try { await connection.invoke('SubmitReading', Number(byId('sensor').value), Number(byId('value').value), byId('source').value); }
        catch (error) { showError(error); }
    });
    async function start() {
        clearTimeout(retry);
        try { await connection.start(); state('Подключено', true); byId('error').hidden = true; await join(); }
        catch (error) { state('Нет связи', false); showError(error); retry = setTimeout(start, 5000); }
    }
    start();
})();
