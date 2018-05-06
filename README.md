# Kinect2Row
Application for the registration, analysis, and display of indoor rowing motion in real-time with Kinect 2.

<img src="https://github.com/noxxomatik/Kinect2Row/blob/master/screenshots/kinect2row_traineeview.png"/>

## Abstract
Low cost markerless motion capture devices like the second generation Kinect are a promising tool for motion recognition, especially since
the data is available in real-time. With Kinect2Row we present a modular analysis application for indoor rowing motion. Our multithreaded
processing pipeline gathers three dimensional joint data from the Kinect, segments the data streams into rowing strokes, calculates typical rowing
measures and displays the results for the trainee. Despite the focus on indoor rowing, several modules of the pipeline can be reused for other
movement patterns and future analysis systems.

## Processing Pipeline
<img src="https://github.com/noxxomatik/Kinect2Row/blob/master/screenshots/processing_network.png"/>

## Preview
View for trainees:

<img src="https://github.com/noxxomatik/Kinect2Row/blob/master/screenshots/kinect2row_traineeview.gif"/>

Extended view for coaches:

<img src="https://github.com/noxxomatik/Kinect2Row/blob/master/screenshots/kinect2row_coachview.gif"/>
