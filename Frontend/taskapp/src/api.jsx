import axios from "axios";

const API_URL = "http://localhost:5218"; // your API port

let token = null;
export function setToken(t) { 
  token = t; 
  localStorage.setItem("token", t);
}
export function getToken() { 
  return token || localStorage.getItem("token"); 
}

function authHeaders() {
  return { Authorization: `Bearer ${getToken()}` };
}

export async function register(username, password) {
  return axios.post(`${API_URL}/register`, { username, password });
}

export async function login(username, password) {
  const res = await axios.post(`${API_URL}/login`, { username, password });
  setToken(res.data.token);
  return res.data;
}

export async function me() {
  return axios.get(`${API_URL}/me`, { headers: authHeaders() });
}

export async function getTodos() {
  return axios.get(`${API_URL}/todos`, { headers: authHeaders() });
}

export async function addTodo(title) {
  return axios.post(`${API_URL}/todos`, { title, done: false }, { headers: authHeaders() });
}

export async function updateTodo(id, todo) {
  return axios.put(`${API_URL}/todos/${id}`, todo, { headers: authHeaders() });
}

export async function deleteTodo(id) {
  return axios.delete(`${API_URL}/todos/${id}`, { headers: authHeaders() });
}