import paho.mqtt.client as mqtt
import json
from datetime import datetime, timedelta
from geopy import distance
import requests

home = (51.5054, -0.0754)

check_distance = 250
land_distance = 45
check_interval = 3

prediction_url = "https://api.v2.sondehub.org/predictions?vehicles="
alert_url = "https://home/api/webhook/sonde-alert"

sondes = {}

class Sonde:
	def __init__(self, lat, lng):
		self.lat = lat
		self.lng = lng
		self.added = datetime.now()
		self.last_checked = datetime.now() - timedelta(minutes = 5)

def on_connect(client, userdata, flags, rc):
	print("Connected with result code " + str(rc))
	client.subscribe("sondes/#")

def on_message(client, userdata, msg):
	data = json.loads(msg.payload)
	if data["serial"] not in sondes:
		sondes[data["serial"]] = Sonde(data["lat"], data["lon"])
	update_sonde(data["serial"], data["lat"], data["lon"])

def update_sonde(serial, lat, lng):
	#print("Updating Sonde: " + serial)
	sondes[serial].lat = lat
	sondes[serial].lng = lng
	if sondes[serial].last_checked < datetime.now() - timedelta(minutes = check_interval):
		check_sonde(serial)

def check_sonde(serial):
	print("Checking Sonde: " + serial)
	coords = (sondes[serial].lat, sondes[serial].lng)
	dist = distance.distance(coords, home).km
	if dist < check_distance:
		jsonstr = requests.get(prediction_url + serial)
		data = json.loads(jsonstr.text)
		veh = data[0]
		points = json.loads(veh["data"])

		points.sort(key=lambda kv: kv["time"])
		landing_point = points[-1]

		landing_coords = (landing_point["lat"], landing_point["lon"])
		landing_dist = distance.distance(landing_coords, home).km
		if landing_dist < land_distance:
			send_alert(serial, landing_point["lat"], landing_point["lon"], landing_dist)
			print("Sonde " + serial + " is predicted to land " + str(round(landing_dist)) + "km away.")

	sondes[serial].last_checked = datetime.now()
	cleanup()

def send_alert(serial, lat, lng, dist):
	requests.post(alert_url + "?dist=" + str(round(dist)) + "&lat=" + str(round(lat, 5)) + "&lng=" + str(round(lng, 5)))

def cleanup():
	for key in sondes:
		if sondes[key].added < datetime.now() - timedelta(hours = 1):
			print ("Sonde " + sondes[key].serial + " is old. Removing from cache.")
			sondes.remove(key)

client = mqtt.Client(transport="websockets")
client.on_connect = on_connect
client.on_message = on_message

client.connect("ws-reader.v2.sondehub.org", 80, 60)

client.loop_forever()
