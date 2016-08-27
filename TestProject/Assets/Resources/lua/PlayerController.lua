import("UnityEngine")

PlayerController = class("PlayerController", function() return LuaLu.LuaComponent.new() end)

function PlayerController:Awake()
	self.countText = GameObject.Find("CountText"):GetComponent("Text")
	self.speed = 10
	self.rb = self:GetComponent(typeof(Rigidbody))
	self.count = 0
end

function PlayerController:Start()
	self:setCountText()
end

function PlayerController:setCountText()
	self.countText.text = "Count:" .. self.count
end

function PlayerController:FixedUpdate()
	local moveHorizontal = Input.GetAxis("Horizontal")
	local moveVertical = Input.GetAxis("Vertical")
	local movement = Vector3.new(moveHorizontal, 0, moveVertical) * self.speed
	self.rb:AddForce(movement)
end

function PlayerController:OnTriggerEnter(other)
	if other.gameObject.tag == "PickUp" then
		self.count = self.count + 1
		other.gameObject:SetActive(false)
		self:setCountText()
	end
end
