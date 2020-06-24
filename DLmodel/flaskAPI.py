import flask
from flask import Flask, request, Response,jsonify
import numpy as np
import keras.models
from keras.models import load_model
import re
import sys 
import os
import tensorflow.compat.v1 as tf
tf.disable_v2_behavior()
import cv2
from keras.utils.generic_utils import CustomObjectScope
from PIL import Image


app = Flask(__name__)

# max image size limit set to 4MB
app.config['MAX_CONTENT_LENGTH'] = 4 * 1024 * 1024 


@app.route('/predict',methods=['POST'])
def predict():
	imgData = request.files['file']
	upload_path=os.path.join("./", imgData.filename)
	imgData.save(upload_path)
	image = cv2.imread("./"+imgData.filename)
	image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
	image = cv2.resize(image, (224, 224))
	image = np.expand_dims(image, axis=0)
	data = np.asarray(image)
	data=data.astype('float32')
	data=data/255.0
	with CustomObjectScope({'relu6': keras.layers.advanced_activations.ReLU(6)}):
			model=load_model("./covidmodel.h5" )
	pred_proba = model.predict(data)
	max_proba=np.argmax(pred_proba,axis=1)
	res=jsonify(result = "noncovid" if max_proba[0]==0 else "covid" )
	return res	
	

if __name__ == '__main__':
    app.run(debug=True, port=8000)
