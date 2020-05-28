RegisterPluginVersion(1,1,1)
Dim info As String = "
Flex-position. Copies logic from CSS3 / HTML5.
Developer: Dmitry Dudin.
Version 1.1 (06 february 2020)
"

'SETTING
Dim treshhold As Double = 0.001

'STUFF
Dim c_gabarit As Container
Dim children As Array[Container]
Dim min_gap_param, mult_gap_param, shift_gap_param, gap_shift, power_magnetic_gap, gaps, freespace As Double
Dim gabarit, child_gabarit, v1, v2, child_v1, child_v2, item_gabarit As Vertex
Dim mode_axis, mode_gabarit_source, mode_justify, mode_align As Integer
Dim width_step, sum_children_width, sum_children_height, gap, start As Double
Dim arr_width, arr_height, arr_shift_x, arr_shift_y As Array[Double]
Dim count As Integer = 1

'INTERFACE
Dim arr_axis As Array[String]
arr_axis.Push(" X ")
arr_axis.Push(" Y ")
Dim arr_gabarit_source As Array[String]
arr_gabarit_source.Push("whole container")
arr_gabarit_source.Push("first sub-container")
Dim arr_justify As Array[String]
arr_justify.Push("start")
arr_justify.Push("end")
arr_justify.Push("center")
arr_justify.Push("space-between")
arr_justify.Push("space-around")
Dim arr_align As Array[String]
arr_align.Push("free")
arr_align.Push("min")
arr_align.Push("center")
arr_align.Push("max")

sub OnInitParameters()
	RegisterParameterContainer("gabarit", "Area")
	RegisterRadioButton("axis", "Axis", 0, arr_axis)
	RegisterRadioButton("gabarit_source", "Size of children", 0, arr_gabarit_source)
	RegisterRadioButton("justify", "Justify", 0, arr_justify)
	RegisterRadioButton("align", "Align", 0, arr_align)
	RegisterParameterDouble("mult_gap", "Multiply gap, %", 0, -1000, 1000)
	RegisterParameterDouble("power_gap", "Magnetic gap", 0, -100, 10000)
	RegisterParameterDouble("min_gap", "Min gap", 0, 0, 1000)
	RegisterParameterDouble("shift_gap", "Shift gap", 0, -1000, 1000)
	RegisterParameterBool("collapse_if_overflow", "Collapse gap if overflow", true)
end sub

sub OnInit()
	c_gabarit = GetParameterContainer("gabarit")
	mode_axis = GetParameterInt("axis")
	mode_gabarit_source = GetParameterInt("gabarit_source")
	mode_justify = GetParameterInt("justify")
	mode_align = GetParameterInt("align")
	mult_gap_param = GetParameterDouble("mult_gap")
	power_magnetic_gap = GetParameterDouble("power_gap")/100.0 + 1.0
	shift_gap_param = GetParameterDouble("shift_gap")
	min_gap_param = GetParameterDouble("min_gap")
end sub
sub OnParameterChanged(parameterName As String)
	OnInit()
	
	if mode_justify == 4 then
		SendGuiParameterShow("mult_gap", SHOW)
		SendGuiParameterShow("power_gap", SHOW)
	else
		SendGuiParameterShow("mult_gap", HIDE)
		SendGuiParameterShow("power_gap", HIDE)
	end if
end sub

'''''''''''''''''''''''''''''''''''''''''''''''''''''

Function GetChildGabarit(child As Container) As Vertex
	if mode_gabarit_source == 1 AND child.ChildContainerCount > 0 then
		GetChildGabarit = child.FirstChildContainer.GetBoundingBoxDimensions()
		GetChildGabarit.x *= child.scaling.x * child.FirstChildContainer.scaling.x
		GetChildGabarit.y *= child.scaling.y * child.FirstChildContainer.scaling.y
	else
		GetChildGabarit = child.GetBoundingBoxDimensions()
		GetChildGabarit.x *= child.scaling.x
		GetChildGabarit.y *= child.scaling.y
	end if
End Function

Sub CalcGapAndStart(gabarit_size As Double, sum_children_size As Double)
	freespace = gabarit_size-sum_children_size
	gap_shift = freespace*(mult_gap_param/100)/(count-1)
	
	'CALC BASE GAP
	Select Case mode_justify
	Case 0 To 2
		'0 - start
		'1 - end
		'2 - center
		gap = 0 + gap_shift
	Case 3
		'3 - space-between
		gap = freespace/(count-1)
	Case 4
		'4 - space-around
		gap = freespace/(count+1)
		'I want the expecting accurate of position with "Shift of gap" = 100.0
		gap += (2*gap)/(count-1)*(mult_gap_param/100)
	End Select
	
	gaps = gap*(count-1)
	
	'ADD MAGNETIC
	Select Case mode_justify
	Case 0 To 2
		'0 - start
		'1 - end
		'2 - center
		gap = (freespace/(count-1))*(gaps/freespace)^power_magnetic_gap
	Case 3
		'3 - space-between
		'no magnetic!
	Case 4
		'4 - space-around
		gap = (freespace/(count-1))*(gaps/freespace)^power_magnetic_gap
	End Select
	
	'CONSIDER MIN GAP
	if gap < min_gap_param then
		if GetParameterBool("collapse_if_overflow") AND (gabarit_size - sum_children_size)/(count-1) < min_gap_param then
			gap = 0
		else
			gap = min_gap_param
		end if
	end if
	gap += shift_gap_param
	
	'CACL START SHIFT
	Select Case mode_justify
	Case 0
		'start
		start = 0
	Case 1
		'end
		start = gabarit_size - sum_children_size - gap*(count-1)
	Case 2 to 4
		'2 - center
		'3 - space-between
		'4 - space-around
		start = (gabarit_size - sum_children_size)/2.0 - gap*(count-1)/2.0
	End Select
End Sub

Dim tv1, tv2 As Vertex
Sub Update()
	c_gabarit.GetTransformedBoundingBox(v1,v2)
	v1 = this.WorldPosToLocalPos(v1)
	v2 = this.WorldPosToLocalPos(v2)
	
	v1.x = (v1.x - this.position.x)/this.scaling.x
	v1.y = (v1.y - this.position.y)/this.scaling.y
	v1.z = (v1.z - this.position.z)/this.scaling.z
	v2.x = (v2.x - this.position.x)/this.scaling.x
	v2.y = (v2.y - this.position.y)/this.scaling.y
	v2.z = (v2.z - this.position.z)/this.scaling.z
	
	gabarit.x = v2.x - v1.x
	gabarit.y = v2.y - v1.y
	
	children.clear
	for i=0 to this.ChildContainerCount-1
		item_gabarit = this.GetChildContainerByIndex(i).GetTransformedBoundingBoxDimensions()
		
		if this.GetChildContainerByIndex(i).active AND item_gabarit.X > treshhold AND item_gabarit.Y > treshhold then
			children.push(this.GetChildContainerByIndex(i))
		end if
	next
	
	arr_width.clear
	arr_height.clear
	arr_shift_x.clear
	arr_shift_y.clear
	sum_children_width = 0
	sum_children_height = 0
	for i=0 to children.UBound
		SetChildrenVertexes(children[i])   'set child_v1 and child_v2
		child_gabarit = GetChildGabarit(children[i])
		
		arr_width.push(child_gabarit.X)
		arr_height.push(child_gabarit.Y)
		sum_children_width  += child_gabarit.X
		sum_children_height += child_gabarit.Y
		
		arr_shift_x.push( sum_children_width  + (child_v1.x - children[i].center.x)*children[i].scaling.x )
		arr_shift_y.push( sum_children_height + (child_v1.y - children[i].center.y)*children[i].scaling.y )
	next
	
	count = children.size
	if mode_axis == 0 then 'X
		CalcGapAndStart(gabarit.X, sum_children_width)
	elseif mode_axis == 1 then 'Y
		CalcGapAndStart(gabarit.Y, sum_children_height)
	end if
End Sub

Sub SetChildrenVertexes(child As Container)
	if mode_gabarit_source == 1 AND child.ChildContainerCount > 0 then
		'first sub-container of child
		child.FirstChildContainer.GetBoundingBox(child_v1, child_v2)
		child_v1.x *= child.FirstChildContainer.scaling.x
		child_v1.y *= child.FirstChildContainer.scaling.y
		child_v2.x *= child.FirstChildContainer.scaling.x
		child_v2.y *= child.FirstChildContainer.scaling.y
		child_v1 += child.FirstChildContainer.position.xyz
		child_v2 += child.FirstChildContainer.position.xyz
		child_v1 = child_v1
	else
		'whole child
		child.GetBoundingBox(child_v1, child_v2)
	end if
End Sub

'''''''''''''''''''''''''''''''''''''''''''''''''''''

sub OnExecPerField()
	Update()
	for i=0 to children.UBound
		SetChildrenVertexes(children[i])   'set child_v1 and child_v2
		
		If mode_axis == 0 then
			'X
			children[i].Position.X = v1.x + start + i*gap + arr_shift_x[i]
			Select Case mode_align
			Case 1
				'min align
				children[i].Position.Y = v1.y - child_v1.y*children[i].scaling.y
			Case 2
				'center align
				children[i].Position.Y = (v1.y+v2.y)/2.0 - (child_v1.y+child_v2.y)*children[i].scaling.y/2.0
			Case 3
				'max align
				children[i].Position.Y = v2.y - child_v2.y*children[i].scaling.y
			End Select 
		Elseif mode_axis == 1 then
			'Y
			children[i].Position.Y = v2.y - start - i*gap - arr_shift_y[i]
			Select Case mode_align
			Case 1
				'min align
				children[i].Position.X = v1.x - child_v1.x*children[i].scaling.x
			Case 2
				'center align
				children[i].Position.X = (v1.x+v2.x)/2.0 - (child_v1.x+child_v2.x)*children[i].scaling.x/2.0
			Case 3
				'max align
				children[i].Position.X = v2.x - child_v2.x*children[i].scaling.x
			End Select
		end if
	next
end sub
