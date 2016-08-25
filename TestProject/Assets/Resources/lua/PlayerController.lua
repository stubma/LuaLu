import("UnityEngine")

PlayerController = class("PlayerController", function() return LuaLu.LuaComponent.new() end)

function PlayerController:Start()
  self.countText = GameObject.Find("CountText")
  self.speed = 10
  self.rb = self:GetComponent("Rigidbody")
  self.count = 0
end

function PlayerController:setCountText()
  self.countText.text = "Count:" .. count
end

function PlayerController:FixedUpdate()
  local moveHorizontal = Input.GetAxis("Horizontal")
  local moveVertical = Input.GetAxis("Vertical")
  local movement = Vector3.new(moveHorizontal, 0, moveVertical)
  movement.x = movement.x * self.speed
  movement.y = movement.y * self.speed
  movement.z = movement.z * self.speed
  self.rb:AddForce(movement)
end

function PlayerController:OnTriggerEnter(...)
  local collider = ...
  if collider.gameObject.tag == "PickUp" then
    self.count = self.count + 1
    collider.gameObject:SetActive(false)
    self:setCountText()
  end
end