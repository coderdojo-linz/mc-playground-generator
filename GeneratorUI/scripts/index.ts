import axios from 'axios';

const submitButton = <HTMLButtonElement>document.getElementById('submit-button');
const username = <HTMLInputElement>document.getElementById('username');
const errorDiv = <HTMLDivElement>document.getElementsByClassName('error-text')[0];
const infoDiv = <HTMLDivElement>document.getElementsByClassName('info-text')[0];
const spinner = <HTMLDivElement>document.getElementById('spinner');
const input = <HTMLDivElement>document.getElementById('input');
const output = <HTMLDivElement>document.getElementById('output');

const host = 'https://mc-playground-generator.azurewebsites.net';
let pin = '';

username.onkeydown = (event: KeyboardEvent) => {
  if (event.key === 'Enter') {
    (<any>window).submit();
  }
};

(<any>window).onLoad = function onLoad() {
  isBusy(false);
  username.focus();
  output.style.display = 'none';

  const params = new URLSearchParams(window.location.search);

  // get pin from url
  if (params.has('pin')) {
    pin = params.get('pin');
  } else {
    errorDiv.innerText = 'Die URL ist nicht korrekt. Der PIN wurde nicht übergeben.';
    submitButton.disabled = true;
  }
};

(<any>window).submit = async function submit() {
  errorDiv.innerText = '';
  infoDiv.innerText = '';

  if (!username.value) {
    errorDiv.innerText = 'Gib bitte einen Benutzernamen ein.';
    return;
  }

  // generate playground request
  try {
    isBusy(true);
    infoDiv.innerText = 'Playground anlegen wird gestartet ...';

    const response = await axios.post(`${host}/api/Generate`, { minecraftUser: username.value, pin: pin });
    infoDiv.innerText = 'Der Playground wird erstellt. Hab bitte ein paar Minuten Geduld ...';

    setTimeout(() => check(response.data.name), 5000);
  } catch (error) {
    isBusy(false);

    if (error.response?.data?.detail) {
      infoDiv.innerText = '';
      errorDiv.innerText = error.response.data.detail;
    } else {
      console.error(error.response);
      infoDiv.innerText = '';
      errorDiv.innerText = 'Beim Erstellen des Playgrounds ist ein Fehler aufgetreten.';
    }
  }
};

async function check(deploymentName: string) {
  // check status
  try {
    const response = await axios.get(`${host}/api/DeploymentStatus?name=${deploymentName}`);

    if (response.data?.properties?.provisioningState === 'Running') {
      setTimeout(() => check(deploymentName), 5000);
    } else {
      // success
      isBusy(false);

      input.style.display = 'none';
      output.style.display = 'block';
      infoDiv.innerText = '';

      const link = `http://${deploymentName}.westeurope.azurecontainer.io:8080/?folder=/app/server/scriptcraft`;
      output.innerHTML = `<p>Dein Playground wurde erfolgreich erstellt:</p><p><a href='${link}' target='_blank'>${link}</a></p><p>Minecraft-Servername: 
      <span class="servername font-mc-five" onclick="navigator.clipboard.writeText('${deploymentName}');" title="Klicke, um den Servernamen zu kopieren.">${deploymentName}</span></p>`;
    }
  } catch (error) {
    if (error.response) {
      console.error(error.reponse);
    } else {
      console.error(error);
    }

    isBusy(false);
    infoDiv.innerText = '';
    errorDiv.innerText = 'Beim Erstellen des Playgrounds ist ein Fehler aufgetreten.';
  }
}

function isBusy(value: boolean) {
  if (value) {
    spinner.style.display = 'inline-block';
    submitButton.disabled = true;
    username.disabled = true;
  } else {
    spinner.style.display = 'none';
    submitButton.disabled = false;
    username.disabled = false;
  }
}
