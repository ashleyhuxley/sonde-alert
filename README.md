# Sonde Alert

Telegram bot to send alerts about radio sondes that are predicted to land within a certain radius of a given location.

## What is a Radio Sonde?

It's the business end of a weather baloon. The thing that gathers and transmits data such as temperature, humidity, pressure, etc.

## But why?

Locating and collecting downed sondes is a hobby. It involves tracking the sonde as it is in flight, then visiting the landing location and trying to find the sonde itself, usually with the help of directional radio equipment. The sonde itself is a trophy.

## Don't the weather service want them back?

No. There are disposal instructions written on them.

## So how does it work?

The lovely people at [SondeHub](https://sondehub.org), one of the major radio sonde tracking sites, helpfully provides the data over [MQTT](https://mqtt.org/). This worker service listens for predictions, finds the predicted landing location and checks the distance of the landing location to the configured home coordinates. If it's within a specified range, the bot sends a Telegram alert.

## What are the configuration options?

- **SondeHubMqttUrl**: THe URL of the SondeHub MQTT server. Usually ws-reader.v2.sondehub.org
- **HomeLat**: The GPS coordinate latitude value of the home coordinates
- **HomeLon**: The GPS coordinate longitude value of the home coordinates
- **AlertRangeKm**: The maximum distance (in kilometers) from the home coordinates that a predicted landing point should be in order to trigger an alert.
- **TelegramBotApiKey**: The API key of the telegram bot, speak to the @BotFather account to set up a new bot and get this.
- **Subscribers**: A list of telegram Chat IDs to send alerts to. Chat IDs can be found by talking to the bot - the bot will log the Chat ID as an information message.

## Libraries Used

- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) 
- [MQTTnet](https://github.com/dotnet/MQTTnet)